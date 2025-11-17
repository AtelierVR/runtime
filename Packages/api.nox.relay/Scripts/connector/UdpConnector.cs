using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Buffer = Nox.CCK.Utils.Buffer;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.relay.connector {
	public class UdpConnector : IConnector {
		private          UdpClient               _udpClient;
		private          Thread                  _receiveThread;
		private          bool                    _isConnected;
		private          IPEndPoint              _remoteEndPoint;
		private volatile bool                    _shouldStop;
		private readonly ConcurrentQueue<Buffer> _receivedDataQueue = new();

		public static string GetStaticProtocolName()
			=> "udp";

		public string GetProtocolName()
			=> GetStaticProtocolName();

		public bool IsConnected()
			=> _isConnected && _udpClient != null;

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

				// Créer le client UDP et se connecter à l'endpoint distant
				_udpClient = new UdpClient();
				_udpClient.Connect(_remoteEndPoint);

				_isConnected = true;
				_shouldStop  = false;

				// Démarrer le thread de réception
				StartReceiveThread();

				return true;
			} catch (Exception ex) {
				Debug.LogError($"UdpConnector: Échec de connexion - {ex.Message}");
				_isConnected = false;
				return false;
			}
		}

		private int _mtuSize = 1200;

		public void SetMtuSize(int size)
			=> _mtuSize = size;

		public int GetMtuSize()
			=> _mtuSize;

		public async UniTask Close() {
			_shouldStop  = true;
			_isConnected = false;

			// Arrêter le thread de réception
			if (_receiveThread is { IsAlive: true })
				_receiveThread.Join(1000); // Attendre 1 seconde maximum

			// Fermer le client UDP
			if (_udpClient != null) {
				try {
					_udpClient.Close();
				} catch (Exception ex) {
					Debug.LogWarning($"UdpConnector: Erreur lors de la fermeture - {ex.Message}");
				} finally {
					_udpClient = null;
				}
			}

			await UniTask.CompletedTask;
		}

		public async UniTask<bool> Send(Buffer buffer) {
			if (!IsConnected())
				return false;

			try {
				var dataToSend = new byte[buffer.length];
				Array.Copy(buffer.data, 0, dataToSend, 0, buffer.length);

				var bytesSent = await _udpClient.SendAsync(dataToSend, dataToSend.Length);
				return bytesSent == buffer.length;
			} catch (Exception ex) {
				Logger.LogError($"UdpConnector: Échec d'envoi - {ex.Message}");
				_isConnected = false;
				return false;
			}
		}

		public void Update() {
			// Cette méthode peut être utilisée pour des opérations de maintenance
			// dans le thread principal Unity si nécessaire

			while (_receivedDataQueue.TryDequeue(out var receivedBuffer))
				OnReceived.Invoke(receivedBuffer);
		}

		private void StartReceiveThread() {
			_receiveThread = new Thread(ReceiveThreadWorker) {
				IsBackground = true,
				Name         = "UdpConnector-Receive"
			};
			_receiveThread.Start();
		}

		private void ReceiveThreadWorker() {
			while (!_shouldStop && IsConnected()) {
				try {
					if (_udpClient.Available > 0) {
						// Recevoir les données UDP
						var senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
						var receivedData   = _udpClient.Receive(ref senderEndPoint);

						if (receivedData.Length <= 0) continue;
						// Créer un Buffer pour les données reçues
						var receivedBuffer = new Buffer();
						receivedBuffer.data = new byte[receivedData.Length];
						Array.Copy(receivedData, 0, receivedBuffer.data, 0, receivedData.Length);
						receivedBuffer.length = (ushort)receivedData.Length;
						receivedBuffer.offset = 0;

						// Enqueue received data to the thread-safe queue
						_receivedDataQueue.Enqueue(receivedBuffer);
					} else {
						// Pause courte pour éviter une boucle intensive
						Thread.Sleep(1);
					}
				} catch (SocketException ex) {
					if (!_shouldStop) {
						Debug.LogError($"UdpConnector: Erreur de réception - {ex.Message}");
						_isConnected = false;
					}

					break;
				} catch (Exception ex) {
					Debug.LogError($"UdpConnector: Erreur inattendue - {ex.Message}");
					_isConnected = false;
					break;
				}
			}

			Debug.Log("UdpConnector: Thread de réception arrêté");
		}

		// Destructeur pour s'assurer que les ressources sont libérées
		~UdpConnector() {
			Close().Forget();
		}
	}
}