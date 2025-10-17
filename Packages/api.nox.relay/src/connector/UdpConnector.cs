using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.connector {
	public class UdpConnector : IConnector {
		private          Socket               _socket;
		private          IPEndPoint           _endPoint;
		private          SocketAsyncEventArgs _receiveEventArgs;
		private          SocketAsyncEventArgs _sendEventArgs;
		private          byte[]               _receiveBuffer;
		private readonly SemaphoreSlim        _sendSemaphore;
		private          int                  _bufferSize = 2048; // Default buffer size

		public UdpConnector() {
			_receiveBuffer = new byte[_bufferSize];
			_sendSemaphore = new SemaphoreSlim(1, 1);
			InitializeSocketEventArgs();
		}

		private void InitializeSocketEventArgs() {
			_receiveEventArgs = new SocketAsyncEventArgs();
			_receiveEventArgs.SetBuffer(_receiveBuffer, 0, _bufferSize);
			_receiveEventArgs.Completed      += OnReceiveCompleted;
			_receiveEventArgs.RemoteEndPoint =  new IPEndPoint(IPAddress.Any, 0);

			_sendEventArgs           =  new SocketAsyncEventArgs();
			_sendEventArgs.Completed += OnSendCompleted;
		}

		public string GetProtocolName()
			=> GetStaticProtocolName();

		public static string GetStaticProtocolName()
			=> "udp";

		public async UniTask<bool> Connect(string address, ushort port) {
			Logger.LogDebug($"Connecting to {address}:{port}");
			
			try {
				if (!IPAddress.TryParse(address, out var ipAddress)) {
					Logger.LogError($"Invalid IP address: {address}");
					return false;
				}
				
				_endPoint = new IPEndPoint(ipAddress, port);
				_socket   = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

				await _socket.ConnectAsync(_endPoint);
				StartReceiving();
				return true;
			} catch (SocketException e) {
				Logger.LogError($"Failed to connect to {address}:{port} ({e.Message})");
				await Close();
				return false;
			} catch (Exception e) {
				Logger.LogError($"Unexpected error connecting to {address}:{port} ({e.Message})");
				await Close();
				return false;
			}
		}

		private void StartReceiving() {
			if (_socket is not { Connected: true }) return;
			
			try {
				var willRaiseEvent = _socket.ReceiveFromAsync(_receiveEventArgs);
				if (!willRaiseEvent)
					ProcessReceive();
			} catch (ObjectDisposedException) {
				// Socket was disposed, ignore
			} catch (Exception e) {
				Logger.LogError($"Error starting receive: {e.Message}");
			}
		}

		private void OnReceiveCompleted(object sender, SocketAsyncEventArgs e) {
			if (e.SocketError == SocketError.Success) {
				ProcessReceive();
			} else if (e.SocketError != SocketError.OperationAborted)
				Logger.LogError($"Receive error: {e.SocketError}");
		}

		private void ProcessReceive() {
			try {
				if (_receiveEventArgs.BytesTransferred > 0) {
					// Validate received data size
					if (_receiveEventArgs.BytesTransferred > _bufferSize) {
						Logger.LogWarning($"Received packet too large: {_receiveEventArgs.BytesTransferred} bytes");
						StartReceiving();
						return;
					}
					
					var buffer = new Buffer();
					var data   = new byte[_receiveEventArgs.BytesTransferred];
					Array.Copy(_receiveBuffer, 0, data, 0, _receiveEventArgs.BytesTransferred);
					buffer.Write(data);
					buffer.Goto(0);
					OnReceivedEvent?.Invoke(buffer);
				}

				StartReceiving();
			} catch (Exception e) {
				Logger.LogError($"Error processing received data: {e.Message}");
				Close().Forget();
			}
		}

		public async UniTask<bool> Send(Buffer buffer) {
			if (_socket is not { Connected: true }) {
				Logger.LogWarning("Cannot send: socket not connected");
				return false;
			}

			if (buffer.length > _bufferSize) {
				Logger.LogWarning($"Packet too large for UDP: {buffer.length} > {_bufferSize}");
				return false;
			}

			await _sendSemaphore.WaitAsync();
			try {
				_sendEventArgs.SetBuffer(buffer.data, 0, buffer.length);
				_sendEventArgs.RemoteEndPoint = _endPoint;

				var tcs = new UniTaskCompletionSource<bool>();
				_sendEventArgs.UserToken = tcs;

				var willRaiseEvent = _socket.SendToAsync(_sendEventArgs);
				if (!willRaiseEvent)
					return ProcessSend();

				return await tcs.Task;
			} catch (SocketException e) {
				Logger.LogError($"Failed to send data ({e.Message})");
				await Close();
				return false;
			} catch (ObjectDisposedException) {
				Logger.LogWarning("Cannot send: socket disposed");
				return false;
			} catch (Exception e) {
				Logger.LogError($"Unexpected error sending data: {e.Message}");
				await Close();
				return false;
			} finally {
				_sendSemaphore.Release();
			}
		}

		private void OnSendCompleted(object sender, SocketAsyncEventArgs e) {
			try {
				var tcs = (UniTaskCompletionSource<bool>)e.UserToken;
				if (e.SocketError == SocketError.Success) {
					tcs.TrySetResult(true);
				} else {
					Logger.LogError($"Send error: {e.SocketError}");
					tcs.TrySetResult(false);
				}
			} catch (Exception ex) {
				Logger.LogError($"Error in OnSendCompleted: {ex.Message}");
			}
		}

		private bool ProcessSend()
			=> _sendEventArgs.SocketError == SocketError.Success;

		public bool IsConnected()
			=> _socket is { Connected: true };

		// ReSharper disable Unity.PerformanceAnalysis
		public void Update() {
			// La réception est maintenant gérée de manière asynchrone
			// Cette méthode peut être utilisée pour d'autres tâches si nécessaire
		}

		public void SetBufferSize(int size) {
			if (size > 0 && size != _bufferSize) {
				var newSize = Math.Max(size, 1024); // Minimum 1024 bytes
				
				// Only update if we're not connected or if it's a significant change
				if (!IsConnected() || Math.Abs(newSize - _bufferSize) > 512) {
					_bufferSize = newSize;
					_receiveBuffer = new byte[_bufferSize];
					
					// Update the receive event args if they exist
					if (_receiveEventArgs != null) {
						_receiveEventArgs.SetBuffer(_receiveBuffer, 0, _bufferSize);
					}
					
					Logger.LogDebug($"UDP buffer size set to {_bufferSize} bytes");
				}
			}
		}

		public UniTask Close() {
			try {
				_socket?.Close();
			} catch (Exception e) {
				Logger.LogWarning($"Error closing socket: {e.Message}");
			} finally {
				_socket   = null;
				_endPoint = null;
			}

			try {
				_receiveEventArgs?.Dispose();
				_sendEventArgs?.Dispose();
			} catch (Exception e) {
				Logger.LogWarning($"Error disposing event args: {e.Message}");
			} finally {
				_receiveEventArgs = null;
				_sendEventArgs    = null;
			}

			try {
				_sendSemaphore?.Dispose();
			} catch (Exception e) {
				Logger.LogWarning($"Error disposing semaphore: {e.Message}");
			}

			return UniTask.CompletedTask;
		}

		public IPEndPoint Remote()
			=> _endPoint;

		public event IConnector.OnReceived OnReceivedEvent;
	}
}