using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using api.nox.relay.connection;
using api.nox.relay.types.Authentication;
using api.nox.relay.types.Traveling;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Nox.CCK.Mods.Events;
using Nox.CCK.Utils;
using Nox.Sessions;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.relay {
	public class Adapting {
		public static void OnCanMakeAdapter(EventData context) {
			if (!context.TryGet<string>(0, out var type) || type is not "external:relay") return;
			var options = !context.TryGet<Dictionary<string, object>>(1, out var opts)
				? new Dictionary<string, object>()
				: opts;

			var conn = options.TryGetValue("data", out var data) && data is JObject jToken
				? jToken
				: null;

			if (conn == null) {
				context.Callback(false);
				return;
			}

			// verify that connections is string[]
			if (!conn.TryGetValue("a", out var ao) || ao is not JArray ja) {
				Logger.LogError("Relay adapter requires 'a' to be set in options as JArray");
				return;
			}

			// Convert JArray to string array
			string[] a;
			try {
				a = ja.ToObject<string[]>();
			} catch {
				Logger.LogError("Relay adapter requires 'a' to be set in options as string[]");
				return;
			}

			if (a == null) {
				Logger.LogError("Relay adapter requires 'a' to be set in options as string[]");
				return;
			}

			// verify that elements is <proto>://<address>:<port>
			var regex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z][a-zA-Z0-9+.-]*://[a-zA-Z0-9.-]+:\d+$");
			foreach (var connection in a)
				if (!regex.IsMatch(connection)) {
					Logger.LogError($"Relay adapter requires 'connections' to be set in options as string[] with elements in format <proto>://<address>:<port>, got '{connection}'");
					return;
				}

			if (a.Length == 0) {
				Logger.LogError("Relay adapter requires 'connections' to be set in options as string[] with at least one connection");
				return;
			}

			// verify that server is string
			if (!options.TryGetValue("server", out var serverObj) || serverObj is not string) {
				Logger.LogError("Relay adapter requires 'server' to be set in options as string");
				return;
			}

			// verify that instance is uint
			if (!options.TryGetValue("instance", out var instanceObj) || instanceObj is not uint) {
				Logger.LogError("Relay adapter requires 'instance' to be set in options as uint");
				return;
			}

			context.Callback(true);
		}

		public static void OnMakeAdapter(EventData context) {
			if (!context.TryGet<string>(0, out var type) || type is not "external:relay") return;

			var options = !context.TryGet<Dictionary<string, object>>(1, out var opts)
				? new Dictionary<string, object>()
				: opts;

			var conn = options.TryGetValue("data", out var data) && data is JObject jToken
				? jToken
				: null;

			var connections = Array.Empty<string>();
			if (conn != null && conn.TryGetValue("a", out var ao) && ao is JArray ja) {
				try {
					var converted = ja.ToObject<string[]>();
					if (converted != null) {
						connections = converted;
					}
				} catch {
					// Log error but continue with empty array
					Logger.LogError("Failed to convert 'a' to string array in OnMakeAdapter");
				}
			}

			var address = options.TryGetValue("address", out var a0)
				&& a0 is string a1
					? a1
					: null;
			var setCurrent = !options.TryGetValue("set_current", out var c0)
				|| c0 is not bool c1
				|| c1;
			var server = options.TryGetValue("server", out var s0)
				&& s0 is string s1
					? s1
					: null;
			var instance = options.TryGetValue("instance", out var i0)
				&& i0 is uint i1
					? i1
					: 0u;

			var name = options.TryGetValue("name", out var t0)
				&& t0 is string t1
					? t1
					: null;

			var shortname = options.TryGetValue("short_name", out var n0)
				&& n0 is string n1
					? n1
					: null;

			var adapter = new RelayAdapter { Name = name, ShortName = shortname };
			var session = Main.SessionAPI.New(adapter);
			adapter.SetState(false, "Preparing relay session...", 0f);
			context.Callback(adapter, session);
			PrepareAsync(session, adapter, connections, address, server, instance, setCurrent).Forget();
			if (options.TryGetValue("thumbnail", out var th0))
				PrepareThumbnailAsync(adapter, th0).Forget();
		}

		private static async UniTask PrepareThumbnailAsync(RelayAdapter adapter, object obj)
			=> adapter.Thumbnail = obj switch {
				string url when Uri.TryCreate(url, UriKind.Absolute, out _) => await Main.NetworkAPI.FetchTexture(url),
				UniTask<Texture2D> tex                                      => await tex,
				Texture2D t                                                 => t,
				_                                                           => null
			};

		private static async UniTask<(string, IPEndPoint)> ParseIPEndPoint(string address) {
			var uri     = new Uri(address);
			var uriType = Uri.CheckHostName(uri.Host);

			switch (uriType) {
				case UriHostNameType.IPv4 or UriHostNameType.IPv6:
					return (uri.Scheme, new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port));
				case UriHostNameType.Dns: {
					var ip = await Dns.GetHostAddressesAsync(uri.Host);
					if (ip.Length > 0)
						return (uri.Scheme, new IPEndPoint(ip[0], uri.Port));
					break;
				}
			}

			return (null, null);
		}

		private static async UniTask PrepareAsync(ISession session, RelayAdapter adapter, string[] connections, string address, string server, uint instance, bool setCurrent) {
			adapter.SetState(false, "Fetching token...", 0.05f);
			var token = await Main.UserAPI.GetToken(server);

			if (token == null) {
				adapter.SetState(false, "Failed to fetch token", -1f);
				Logger.LogError($"Failed to fetch token for server {server}");
				await session.Dispose();
				return;
			}

			Connection connection = null;
			foreach (var addr in connections) {
				if (connection != null)
					await connection.Dispose();

				adapter.SetState(false, $"Connecting to {addr}...", 0.1f);
				var (proto, endPoint) = await ParseIPEndPoint(addr);

				connection = Main.Instance.GetConnectionByType(proto);
				if (connection == null) {
					Logger.LogWarning($"No connection for protocol {proto} found, trying to create a new one");
					continue;
				}

				if (!await connection.Connect(endPoint.Address.ToString(), (ushort)endPoint.Port)) {
					Logger.LogWarning($"Failed to connect to {addr}");
					continue;
				}

				break;
			}


			if (connection == null) {
				adapter.SetState(false, "Failed to connect to any relay", -1f);
				Logger.LogError("Failed to connect to any relay");
				await session.Dispose();
				return;
			}

			adapter.Connection = connection;

			adapter.SetState(false, "Handshaking with relay...", 0.2f);
			var hand = await connection.RequestHandshake();
			if (hand == null) {
				adapter.SetState(false, "Failed to handshake with relay", -1f);
				Logger.LogError("Failed to handshake with relay");
				await session.Dispose();
				return;
			}

			// Configure buffer size based on server's MaxPacketSize
			if (hand.MaxPacketSize > 0) {
				connection.Connector.SetBufferSize(hand.MaxPacketSize);
				Logger.LogDebug($"Set connector buffer size to {hand.MaxPacketSize} bytes from handshake");
			}

			adapter.SetState(false, "Authenticating with relay...", 0.225f);
			var request = RelayRequestAuthentication.CreateRequest();
			var auth    = await connection.RequestAuthentication(request);
			if (auth.IsError()) {
				adapter.SetState(false, $"Authentication failed: {auth.Reason}", -1f);
				Logger.LogError($"Authentication failed: {auth.Result} - {auth.Reason}");
				await session.Dispose();
				return;
			}

			var challenge = auth.Challenge;
			Logger.LogDebug($"Received challenge: {challenge.Length}{string.Join(", ", challenge.Select(c => c.ToString("X2")))}");

			var keys = Crypto.GetKeys();
			var sign = Crypto.Sign(challenge, keys);

			var user = Main.UserAPI.GetCurrent();

			request = RelayRequestAuthentication.CreateResponse(
				Crypto.ExportPublicKeyToDer(keys),
				sign,
				user?.GetId()            ?? 0,
				user?.GetServerAddress() ?? "::"
			);

			auth = await connection.RequestAuthentication(request);
			if (auth.IsError()) {
				adapter.SetState(false, $"Authentication failed: {auth.Reason}", -1f);
				Logger.LogError($"Authentication failed: {auth.Result} - {auth.Reason}");
				await session.Dispose();
				return;
			}

			adapter.SetState(false, "Fetching instances from relay...", 0.25f);
			adapter.Instance = await connection.RequestSession(instance);
			if (adapter.Instance == null) {
				adapter.SetState(false, $"Failed to get instance {instance}", -1f);
				Logger.LogError($"Failed to get instance {instance} from relay");
				await session.Dispose();
				return;
			}

			adapter.Instance.OnQuit.AddListener(adapter.OnQuit);
			adapter.Instance.OnJoin.AddListener(adapter.OnJoin);
			adapter.Instance.OnLeave.AddListener(adapter.OnLeave);
			adapter.Instance.OnAvatarChanged.AddListener(adapter.OnAvatarChanged);
			adapter.Instance.OnTransform.AddListener(adapter.OnTransform);
			adapter.Instance.OnAvatarParams.AddListener(adapter.OnAvatarParams);
			adapter.Instance.OnPlayerUpdated.AddListener(adapter.OnPlayerUpdated);

			adapter.SetState(false, "Connecting to an instance...", 0.3f);
			var enter = await adapter.Instance.RequestEnter();
			if (enter.IsError) {
				adapter.SetState(false, "Failed to connect to instance", -1f);
				Logger.LogError($"Failed to connect to instance {instance}: {enter.Result} - {enter.Reason}");
				await session.Dispose();
				return;
			}

			adapter.Tps       = enter.Tps;
			adapter.Threshold = enter.Threshold;

			adapter.SetState(false, $"Connected as {enter.Player.Display} ({enter.Player.Id}) to instance {instance}", 0.325f);

			var travalRequest = await adapter.Instance.RequestTraveling(TravelingAction.Travel);
			if (!travalRequest.IsSuccess) {
				adapter.SetState(false, $"Failed to travel to instance {instance}: {travalRequest.Reason}", -1f);
				Logger.LogError($"Failed to travel to instance {instance}: {travalRequest.Results} - {travalRequest.Reason}");
				await session.Dispose();
				return;
			}

			var traveling = await adapter.OnTravelingAsync(
				travalRequest,
				autoResponse: false,
				progress: (f, s) => adapter.SetState(false, s, 0.325f + f * 0.575f)
			);

			if (!traveling) {
				adapter.SetState(false, "Failed to travel to instance", -1f);
				Logger.LogError($"Failed to travel to instance {instance}");
				await session.Dispose();
				return;
			}

			adapter.SetState(false, $"Making ready in instance {instance}...", 0.9f);

			var travelReady = await adapter.Instance.RequestTraveling(TravelingAction.Ready);
			if (!travelReady.IsReady) {
				adapter.SetState(false, $"Failed to travel to instance {instance}: {travelReady.Reason}", -1f);
				Logger.LogError($"Failed to travel to instance {instance}: {travelReady.Results} - {travelReady.Reason}");
				await session.Dispose();
				return;
			}

			adapter.Instance.OnTraveling.AddListener(adapter.OnTraveling);
			adapter.Instance.OnEnter.AddListener(adapter.OnEnter);

			Logger.LogDebug($"Local player: {enter.Player.Display} ({enter.Player.Id}, {enter.Player.Flags})");
			var player = adapter.NewPlayer<RelayLocalPlayer>(enter.Player);

			adapter.SetState(false, "Setting local player avatar...", 0.925f);
			if (player.GetAvatar() != null && !await player.SetAvatar(player.GetAvatar())) {
				adapter.SetState(false, "Failed to set local player avatar", -1f);
				Logger.LogError($"Failed to set local player avatar for instance {instance}");
				await session.Dispose();
				return;
			}

			if (setCurrent) {
				adapter.SetState(false, "Setting instance as current", 0.95f);
				await session.SetCurrent();
			}

			adapter.SetState(true, "Ready", 1f);
		}
	}
}