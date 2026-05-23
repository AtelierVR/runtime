using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Servers;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Text;
using Newtonsoft.Json;
using Nox.Users;
using UnityEngine;
using UnityEngine.Events;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.server.network {
	public class ServerSocket : IServerSocket {
		public static readonly List<ServerSocket> Connections = new();

		// Événements
		public UnityEvent OnConnected { get; } = new();
		public UnityEvent OnDisconnected { get; } = new();
		public UnityEvent<byte[]> OnRaw { get; } = new();
		public UnityEvent<SocketPacket> OnPacket { get; } = new();
		public readonly UnityEvent<string> OnMessageReceived = new();
		public readonly UnityEvent<Exception> OnError = new();

		private bool _autoReconnect = true;
		private bool _reconcilable;
		private bool _isListening;
		private readonly string _address;
		private readonly Uri _url;
		private int _maxRetries = 0;

		private readonly Dictionary<string, string> _headers = new() {
			{
				"User-Agent",
				string.Join(
					' ',
					$"{Application.productName}/{Application.version}",
					$"{Constants.ProtocolIdentifier}/{Constants.ProtocolVersion}",
					$"(en={EngineExtensions.CurrentEngine.GetEngineName()}; pn={PlatformExtensions.CurrentPlatform.GetPlatformName()})"
				)
			}, {
				"X-UUID",
				SystemInfo.deviceUniqueIdentifier
			}, {
				"X-Nox-User",
				Main.UserAPI?.Current?.Identifier.ToString()
				?? string.Empty
			}, {
				"X-Nox-Mods",
				string.Join(
					"; ",
					Main.Instance.CoreAPI.ModAPI.GetMods()
						.Where(mod => mod != null && mod.IsLoaded())
						.Select(m => m.GetMetadata())
						.Select(metadata => $"{metadata.GetId()}/{metadata.GetVersion()}")
				)
			}, {
				"X-Powered-By",
				"Nox"
			}
		};

		private ClientWebSocket _webSocket;
		private CancellationTokenSource _cts;

		// ── Typed handler storage ─────────────────────────────────────────────────
		private readonly Dictionary<string, List<Action<SocketPacket>>> _handlers = new();
		// Maps original typed handler (object) → storage wrapper
		private readonly Dictionary<object, Action<SocketPacket>> _handlerWrappers = new();
		// Maps original handler → once-proxy (both typed, stored as object)
		private readonly Dictionary<object, object> _onceOriginalToWrapper = new();


		public ServerSocket(string server, string uri, IAuthToken authToken = null, Dictionary<string, string> headers = null) {
			_address = server;
			if (headers != null)
				foreach (var header in headers)
					_headers[header.Key] = header.Value;
			if (authToken != null)
				_headers.Add("Authorization", authToken.ToHeader());
			_url = new Uri(uri);
			_reconcilable = false;
			Connections.Add(this);
		}

		public static async UniTask<ServerSocket> Make(string address, IAuthToken auth = null) {
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot connect socket {address}: no server address provided.");
				return null;
			}

			var server = await Main.Instance.Fetch(address);
			if (server == null) {
				Logger.LogError($"Cannot connect socket {address}: no server found at address.");
				return null;
			}

			var uri = server.Gateway?.Ws;
			if (string.IsNullOrEmpty(uri)) {
				Logger.LogError($"Cannot connect socket {address}: no WebSocket URI found.");
				return null;
			}

			var socket = new ServerSocket(address, uri, auth);

			return socket;
		}

		public async UniTask<bool> Connect() {
			if (_cts != null)
				await Close();

			_webSocket = new ClientWebSocket();
			_cts = new CancellationTokenSource();

			// Force-initialise ServicePointManager before the first TLS handshake.
			// In built Unity (Mono) builds the static constructor fails if System.Configuration
			// is stripped; this surfaces the error early and sets the security protocol explicitly.
			try {
				System.Net.ServicePointManager.SecurityProtocol =
					System.Net.SecurityProtocolType.Tls12 |
					(System.Net.SecurityProtocolType)12288; // Tls13 (not defined in Unity's Mono)
			} catch (Exception ex) {
				Logger.LogError(new Exception("Failed to initialise ServicePointManager. TLS connections will not work. Ensure System and System.Configuration are preserved in link.xml.", ex));
			}

			foreach (var header in _headers)
				try {
					_webSocket.Options.SetRequestHeader(header.Key, header.Value);
				} catch (Exception ex) {
					Logger.LogError(new Exception($"Error setting WebSocket header {header.Key}", ex));
				}

			try {
				await _webSocket.ConnectAsync(_url, _cts.Token);
			} catch (Exception ex) {
				Logger.LogError(new Exception($"Error while connecting to WebSocket at {_url}", ex));
				_reconcilable = false;
				OnError?.Invoke(ex);
				return false;
			}

			Logger.LogDebug($"WebSocket connection to {_url} established successfully.");
			_reconcilable = true;
			OnConnected.Invoke();

			// Démarrer l'écoute des messages
			StartListening();

			return true;
		}

		public async UniTask<bool> Close() {
			_isListening = false;

			if (!IsConnected()) {
				Logger.LogDebug("WebSocket is already closed or not initialized.");
				return true;
			}

			try {
				await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection", CancellationToken.None);
			} catch (Exception ex) {
				Logger.LogError(new Exception("Error while closing WebSocket connection", ex));
				OnError?.Invoke(ex);
			}

			Logger.LogDebug($"WebSocket connection to {_url} closed successfully.");

			// Nettoyer les ressources
			_webSocket.Dispose();
			_webSocket = null;
			_cts.Cancel();
			_cts.Dispose();
			_cts = null;
			OnDisconnected.Invoke();

			return true;
		}

		public bool IsConnected()
			=> _webSocket is { State: WebSocketState.Open };

		public bool CanAutoReconnect()
			=> _autoReconnect;

		public void SetAutoReconnect(bool autoReconnect)
			=> _autoReconnect = autoReconnect;

		// Méthodes pour gérer les headers WebSocket
		public void SetHeader(string name, string value)
			=> _headers[name] = value;

		public void RemoveHeader(string name)
			=> _headers.Remove(name);

		public void ClearHeaders()
			=> _headers.Clear();

		public Dictionary<string, string> GetHeaders()
			=> new(_headers);

		private void StartListening() {
			if (_isListening) return;
			_isListening = true;
			ListenForMessages().Forget();
		}

		private async UniTask ListenForMessages() {
			var buffer = new byte[4096];

			while (_webSocket is { State: WebSocketState.Open } && !_cts.Token.IsCancellationRequested) {
				try {
					var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
					if (result.MessageType == WebSocketMessageType.Text) {
						var rawBytes = new byte[result.Count];
						Array.Copy(buffer, rawBytes, result.Count);
						var message = Encoding.UTF8.GetString(rawBytes);
						OnMessageReceived.Invoke(message);
						OnRaw.Invoke(rawBytes);
						TryDispatchPacket(message);
					}
					else if (result.MessageType == WebSocketMessageType.Close) {
						Logger.LogDebug("WebSocket connection closed by server.");
						await HandleDisconnection();
						break;
					}
				} catch (OperationCanceledException) {
					// Connexion fermée normalement
					break;
				} catch (WebSocketException ex) {
					Logger.LogError(new Exception("WebSocket error in listening loop", ex));
					OnError.Invoke(ex);
					await HandleDisconnection();
					break;
				} catch (Exception ex) {
					Logger.LogError(new Exception("Unexpected error in WebSocket listening loop", ex));
					if (OnError != null) OnError.Invoke(ex);
					await HandleDisconnection();
					break;
				}
			}

			_isListening = false;
		}

		private async UniTask HandleDisconnection() {
			_isListening = false;
			OnDisconnected.Invoke();

			if (_autoReconnect && _reconcilable) {
				Logger.LogDebug("Attempting to reconnect...");
				await UniTask.Delay(TimeSpan.FromSeconds(5)); // Attendre 5 secondes avant de reconnecter
				if (_cts is { Token: { IsCancellationRequested: false } })
					await AttemptReconnect();
			}
		}

		private async UniTask AttemptReconnect() {
			var retryCount = 0;

			while ((retryCount < _maxRetries || _maxRetries == 0) && _autoReconnect && (_cts == null || !_cts.Token.IsCancellationRequested)) {
				try {
					retryCount++;
					Logger.LogDebug($"Reconnection attempt {retryCount}/{_maxRetries}");

					if (await Connect()) {
						Logger.LogDebug("Reconnection successful.");
						return;
					}
				} catch (Exception ex) {
					Logger.LogError(new Exception($"Error during reconnection attempt {retryCount}", ex));
					OnError?.Invoke(ex);
				}

				if (retryCount >= _maxRetries && _maxRetries != 0) continue;
				var delay = Math.Min(30, retryCount * 5); // Délai progressif jusqu'à 30 secondes
				await UniTask.Delay(TimeSpan.FromSeconds(delay));
			}

			Logger.LogError($"Failed to reconnect after {_maxRetries} attempts.");
		}

		public async UniTask<bool> SendMessage(string message) {
			if (!IsConnected()) {
				Logger.LogError("Cannot send message: WebSocket is not connected.");
				return false;
			}

			try {
				var buffer = Encoding.UTF8.GetBytes(message);
				await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cts.Token);
				return true;
			} catch (Exception ex) {
				Logger.LogError(new Exception("Unexpected error in sending message", ex));
				OnError.Invoke(ex);
				return false;
			}
		}

		public async UniTask Dispose() {
			if (_cts != null)
				await Close();
			_isListening = false;

			_webSocket?.Dispose();
			_webSocket = null;
			_cts?.Cancel();
			_cts?.Dispose();
			_cts = null;

			Connections.Remove(this);
			Logger.LogDebug($"ServerSocket for {_address} disposed.");
		}

		// ── Packet system ──────────────────────────────────────────────────────────

		private void TryDispatchPacket(string message) {
			try {
				var packet = JsonConvert.DeserializeObject<SocketPacket>(message);
				if (packet?.type == null) return;
				OnPacket.Invoke(packet);
				List<Action<SocketPacket>> snapshot;
				lock (_handlers) {
					if (!_handlers.TryGetValue(packet.type, out var list)) return;
					snapshot = new List<Action<SocketPacket>>(list);
				}
				foreach (var h in snapshot)
					h(packet);
			} catch { /* ignore malformed packets */ }
		}

		private static SocketPacket<T> ToTyped<T>(SocketPacket raw) {
			T typedPayload;
			if (raw.payload is T direct)
				typedPayload = direct;
			else if (raw.payload is Newtonsoft.Json.Linq.JToken jt)
				typedPayload = jt.ToObject<T>();
			else {
				var json = JsonConvert.SerializeObject((object)raw.payload);
				typedPayload = JsonConvert.DeserializeObject<T>(json);
			}
			return new SocketPacket<T> { type = raw.type, id = raw.id, payload = typedPayload };
		}

		public async UniTask Emit<T>(SocketPacket<T> packet) {
			var json = JsonConvert.SerializeObject(packet, new JsonSerializerSettings {
				NullValueHandling = NullValueHandling.Ignore
			});
			await SendMessage(json);
		}

		public void On<T>(string type, Action<SocketPacket<T>> handler) {
			Action<SocketPacket> wrapper = raw => handler(ToTyped<T>(raw));
			lock (_handlers) {
				_handlerWrappers[handler] = wrapper;
				if (!_handlers.TryGetValue(type, out var list))
					_handlers[type] = list = new List<Action<SocketPacket>>();
				list.Add(wrapper);
			}
		}

		public void Off<T>(string type, Action<SocketPacket<T>> handler) {
			lock (_handlers) {
				// If registered via Once, remove the once-proxy instead
				if (_onceOriginalToWrapper.TryGetValue(handler, out var onceObj)) {
					_onceOriginalToWrapper.Remove(handler);
					Off(type, (Action<SocketPacket<T>>)onceObj);
					return;
				}
				if (!_handlerWrappers.TryGetValue(handler, out var wrapper)) return;
				_handlerWrappers.Remove(handler);
				if (_handlers.TryGetValue(type, out var list))
					list.Remove(wrapper);
			}
		}

		public void Once<T>(string type, Action<SocketPacket<T>> handler) {
			Action<SocketPacket<T>> onceProxy = null;
			onceProxy = pkt => {
				Off(type, handler);
				handler(pkt);
			};
			lock (_handlers)
				_onceOriginalToWrapper[handler] = onceProxy;
			On(type, onceProxy);
		}
	}
}