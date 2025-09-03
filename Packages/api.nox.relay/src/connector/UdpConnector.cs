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
		private readonly byte[]               _receiveBuffer;
		private readonly SemaphoreSlim        _sendSemaphore;
		private const    int                  BufferSize = 1024;

		public UdpConnector() {
			_receiveBuffer = new byte[BufferSize];
			_sendSemaphore = new SemaphoreSlim(1, 1);
			InitializeSocketEventArgs();
		}

		private void InitializeSocketEventArgs() {
			_receiveEventArgs = new SocketAsyncEventArgs();
			_receiveEventArgs.SetBuffer(_receiveBuffer, 0, BufferSize);
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
			_endPoint = new IPEndPoint(IPAddress.Parse(address), port);
			_socket   = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

			try {
				await _socket.ConnectAsync(_endPoint);
				StartReceiving();
				return true;
			} catch (SocketException e) {
				Logger.LogError($"Failed to connect to {address}:{port} ({e.Message})");
				await Close();
			}

			return false;
		}

		private void StartReceiving() {
			if (_socket is not { Connected: true }) return;
			var willRaiseEvent = _socket.ReceiveFromAsync(_receiveEventArgs);
			if (!willRaiseEvent)
				ProcessReceive();
		}

		private void OnReceiveCompleted(object sender, SocketAsyncEventArgs e) {
			if (e.SocketError == SocketError.Success) {
				ProcessReceive();
			} else if (e.SocketError != SocketError.OperationAborted)
				Logger.LogError($"Receive error: {e.SocketError}");
		}

		private void ProcessReceive() {
			if (_receiveEventArgs.BytesTransferred > 0) {
				var buffer = new Buffer();
				var data   = new byte[_receiveEventArgs.BytesTransferred];
				Array.Copy(_receiveBuffer, 0, data, 0, _receiveEventArgs.BytesTransferred);
				buffer.Write(data);
				buffer.Goto(0);
				OnReceivedEvent?.Invoke(buffer);
			}

			StartReceiving();
		}

		public async UniTask<bool> Send(Buffer buffer) {
			if (_socket is not { Connected: true }) return false;

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
			} finally {
				_sendSemaphore.Release();
			}
		}

		private void OnSendCompleted(object sender, SocketAsyncEventArgs e) {
			var tcs = (UniTaskCompletionSource<bool>)e.UserToken;
			if (e.SocketError == SocketError.Success) {
				tcs.TrySetResult(true);
			} else {
				Logger.LogError($"Send error: {e.SocketError}");
				tcs.TrySetResult(false);
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

		public UniTask Close() {
			_socket?.Close();
			_socket   = null;
			_endPoint = null;

			_receiveEventArgs?.Dispose();
			_sendEventArgs?.Dispose();
			_receiveEventArgs = null;
			_sendEventArgs    = null;

			_sendSemaphore?.Dispose();

			return UniTask.CompletedTask;
		}

		public IPEndPoint Remote()
			=> _endPoint;

		public event IConnector.OnReceived OnReceivedEvent;
	}
}