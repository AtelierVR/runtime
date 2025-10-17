using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;

namespace api.nox.relay.connector {
	public class TcpConnector : IConnector {
		private          Socket                            _socket;
		private          Thread                            _receiveThread;
		private          bool                              _isConnected;
		private          IPEndPoint                        _remoteEndPoint;
		private          int                               _bufferSize = 1024;
		private volatile bool                              _shouldStop;
		private readonly ConcurrentQueue<Nox.CCK.Utils.Buffer> _receivedDataQueue = new();

		public event IConnector.OnReceived OnReceivedEvent;

		public static string GetStaticProtocolName()
			=> "tcp";

		public string GetProtocolName()
			=> GetStaticProtocolName();

		public bool IsConnected()
			=> _isConnected && _socket is { Connected: true };


		public IPEndPoint Remote()
			=> _remoteEndPoint;


		public async UniTask<bool> Connect(string address, ushort port) {
			try {
				// Nettoyer les connexions précédentes
				await Close();

				// Créer le socket TCP
				_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

				// Parser l'adresse
				if (!IPAddress.TryParse(address, out var ipAddress)) {
					var hostEntry = await Dns.GetHostEntryAsync(address);
					ipAddress = hostEntry.AddressList[0];
				}

				_remoteEndPoint = new IPEndPoint(ipAddress, port);

				// Connexion asynchrone
				await _socket.ConnectAsync(_remoteEndPoint);

				_isConnected = true;
				_shouldStop  = false;

				// Démarrer le thread de réception
				StartReceiveThread();

				return true;
			} catch (Exception ex) {
				Debug.LogError($"TcpConnector: Échec de connexion - {ex.Message}");
				_isConnected = false;
				return false;
			}
		}

		public void SetBufferSize(int size)
			=> _bufferSize = size;


		public async UniTask Close() {
			_shouldStop  = true;
			_isConnected = false;

			// Arrêter le thread de réception
			if (_receiveThread is { IsAlive: true })
				_receiveThread.Join(1000); // Attendre 1 seconde maximum

			// Fermer le socket
			if (_socket != null)
				try {
					_socket.Shutdown(SocketShutdown.Both);
					_socket.Close();
				} catch (Exception ex) {
					Debug.LogWarning($"TcpConnector: Erreur lors de la fermeture - {ex.Message}");
				} finally {
					_socket = null;
				}

			await UniTask.CompletedTask;
		}

		public async UniTask<bool> Send(Nox.CCK.Utils.Buffer buffer) {
			if (!IsConnected())
				return false;

			try {
				var dataToSend = new byte[buffer.length];
				Array.Copy(buffer.data, 0, dataToSend, 0, buffer.length);

				var bytesSent = await _socket.SendAsync(dataToSend, SocketFlags.None);
				return bytesSent == buffer.length;
			} catch (Exception ex) {
				Debug.LogError($"TcpConnector: Échec d'envoi - {ex.Message}");
				_isConnected = false;
				return false;
			}
		}

		public void Update() {
			// Cette méthode peut être utilisée pour des opérations de maintenance
			// dans le thread principal Unity si nécessaire

			while (_receivedDataQueue.TryDequeue(out var receivedBuffer)) 
				OnReceivedEvent?.Invoke(receivedBuffer);
		}

		private void StartReceiveThread() {
			_receiveThread = new Thread(ReceiveThreadWorker) {
				IsBackground = true,
				Name         = "TcpConnector-Receive"
			};
			_receiveThread.Start();
		}

		private void ReceiveThreadWorker() {
			var buffer = new byte[_bufferSize];

			while (!_shouldStop && IsConnected()) {
				try {
					if (_socket.Available > 0) {
						// Utiliser Math.Min pour s'assurer que la taille ne dépasse pas la longueur du buffer
						var maxReceive = Math.Min(_socket.Available, buffer.Length);
						var bytesReceived = _socket.Receive(buffer, 0, maxReceive, SocketFlags.None);

						if (bytesReceived > 0) {
							// Créer un Buffer pour les données reçues
							var receivedBuffer = new Nox.CCK.Utils.Buffer();
							receivedBuffer.data = new byte[bytesReceived];
							Array.Copy(buffer, 0, receivedBuffer.data, 0, bytesReceived);
							receivedBuffer.length = (ushort)bytesReceived;
							receivedBuffer.offset = 0;

							// Enqueue received data to the thread-safe queue
							_receivedDataQueue.Enqueue(receivedBuffer);
						} else if (bytesReceived == 0) {
							// Connexion fermée par le serveur
							_isConnected = false;
							break;
						}
					} else {
						// Pause courte pour éviter une boucle intensive
						Thread.Sleep(1);
					}
				} catch (SocketException ex) {
					if (!_shouldStop) {
						Debug.LogError($"TcpConnector: Erreur de réception - {ex.Message}");
						_isConnected = false;
					}

					break;
				} catch (Exception ex) {
					Debug.LogError($"TcpConnector: Erreur inattendue - {ex.Message}");
					_isConnected = false;
					break;
				}
			}

			Debug.Log("TcpConnector: Thread de réception arrêté");
		}

		// Destructeur pour s'assurer que les ressources sont libérées
		~TcpConnector() {
			Close().Forget();
		}
	}
}