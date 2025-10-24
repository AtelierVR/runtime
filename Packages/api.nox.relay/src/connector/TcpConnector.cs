using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using api.nox.relay.types;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Buffer = Nox.CCK.Utils.Buffer;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.relay.connector {
	public class TcpConnector : IConnector {
		private TcpClient _tcpClient;
		private NetworkStream _stream;
		private bool _isConnected;
		private IPEndPoint _remoteEndPoint;
		private volatile bool _shouldStop;
		private readonly ConcurrentQueue<Buffer> _receivedDataQueue = new();
		private int _bufferSize = 8192;

		public static string GetStaticProtocolName()
			=> "tcp";

		public string GetProtocolName()
			=> GetStaticProtocolName();

		public bool IsConnected()
			=> _isConnected && _tcpClient is { Connected: true };

		public IPEndPoint Remote()
			=> _remoteEndPoint;

		public UnityEvent<Buffer> OnReceived { get; } = new();

		public async UniTask<bool> Connect(string address, ushort port) {
			try {
				// Nettoyer les connexions précédentes
				await Close();

				// Parser l'adresse
				IPAddress ipAddress;
				if (!IPAddress.TryParse(address, out ipAddress)) {
					var hostEntry = await Dns.GetHostEntryAsync(address);
					ipAddress = hostEntry.AddressList[0];
				}

				_remoteEndPoint = new IPEndPoint(ipAddress, port);

				// Créer le client TCP et se connecter
				_tcpClient = new TcpClient();
				await _tcpClient.ConnectAsync(ipAddress, port);
				_stream = _tcpClient.GetStream();

				_isConnected = true;
				_shouldStop = false;

				// Démarrer la lecture asynchrone
				StartReceiveAsync().Forget();

				return true;
			} catch (Exception ex) {
				Debug.LogError($"TcpConnector: Échec de connexion - {ex.Message}");
				_isConnected = false;
				return false;
			}
		}

		public void SetBufferSize(int size) {
			_bufferSize = size;
			if (_tcpClient != null) {
				_tcpClient.ReceiveBufferSize = size;
				_tcpClient.SendBufferSize = size;
			}
		}

		public async UniTask Close() {
			_shouldStop = true;
			_isConnected = false;

			// Fermer le stream
			if (_stream != null) {
				try {
					await _stream.FlushAsync();
					_stream.Close();
				} catch (Exception ex) {
					Debug.LogWarning($"TcpConnector: Erreur lors de la fermeture du stream - {ex.Message}");
				} finally {
					_stream = null;
				}
			}

			// Fermer le client TCP
			if (_tcpClient != null) {
				try {
					_tcpClient.Close();
				} catch (Exception ex) {
					Debug.LogWarning($"TcpConnector: Erreur lors de la fermeture - {ex.Message}");
				} finally {
					_tcpClient = null;
				}
			}

			// Vider la queue
			while (_receivedDataQueue.TryDequeue(out _)) { }
		}

		public async UniTask<bool> Send(Buffer buffer) {
			if (!IsConnected() || _stream == null)
				return false;

			try {
				await _stream.WriteAsync(buffer.data, 0, buffer.length);
				await _stream.FlushAsync();
				return true;
			} catch (Exception ex) {
				Logger.LogError($"TcpConnector: Échec d'envoi - {ex.Message}");
				_isConnected = false;
				return false;
			}
		}

		public void Update() {
			// Traiter les données reçues dans la queue
			while (_receivedDataQueue.TryDequeue(out var buffer)) {
				OnReceived?.Invoke(buffer);
			}
		}

		private async UniTaskVoid StartReceiveAsync() {
			var buffer = new byte[_bufferSize];

			while (!_shouldStop && IsConnected()) {
				try {
					var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);

					if (bytesRead > 0) {
						var receivedBuffer = new Buffer();
						receivedBuffer.data = new byte[bytesRead];
						Array.Copy(buffer, 0, receivedBuffer.data, 0, bytesRead);
						receivedBuffer.length = (ushort)bytesRead;
						receivedBuffer.offset = 0;
						_receivedDataQueue.Enqueue(receivedBuffer);
					} else {
						// Connexion fermée par le serveur
						Debug.Log("TcpConnector: Connexion fermée par le serveur");
						_isConnected = false;
						break;
					}
				} catch (Exception ex) {
					if (!_shouldStop) {
						Debug.LogError($"TcpConnector: Erreur de réception - {ex.Message}");
						_isConnected = false;
					}
					break;
				}
			}
		}
	}
}