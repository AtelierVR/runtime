using System;
using System.Net;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.connector {
	public class TcpConnector : IConnector {
		// TCP client variables
		private TcpClient     _client = null;
		private NetworkStream _stream = null;
		private byte[]        _buffer = new byte[2048]; // Increased buffer size
		private Buffer _receiveBuffer = new(); // Accumulation buffer for partial packets

		public string GetProtocolName()
			=> GetStaticProtocolName();
		
		public static string GetStaticProtocolName()
			=> "tcp";

		public IPEndPoint Remote()
			=> _client?.Client.RemoteEndPoint as IPEndPoint;

		public event IConnector.OnReceived OnReceivedEvent;

		public bool IsConnected()
			=> _client is { Connected: true };

		public UniTask<bool> Connect(string address, ushort port) {
			try {
				_client = new TcpClient(address, port);
				_stream = _client.GetStream();
				_stream.BeginRead(_buffer, 0, _buffer.Length, ReceiveCallback, null);
				return UniTask.FromResult(true);
			} catch (SocketException e) {
				Logger.LogError($"Failed to connect to {address}:{port} ({e.Message})");
				return UniTask.FromResult(false);
			}
		}

		public void Update() { }

		public void SetBufferSize(int size) {
			if (size > 0 && size != _buffer.Length) {
				_buffer = new byte[Math.Max(size, 1024)]; // Minimum 1024 bytes
				Logger.LogDebug($"TCP buffer size set to {_buffer.Length} bytes");
			}
		}

		private void ReceiveCallback(IAsyncResult result) {
			try {
				if (!IsConnected()) return;
				
				var bytesRead = _stream.EndRead(result);
				if (bytesRead == 0) {
					// Connection closed by remote
					Close().Forget();
					return;
				}

				// Add received data to accumulation buffer
				for (var i = 0; i < bytesRead; i++)
					_receiveBuffer.Write(_buffer[i]);

				// Process complete packets from the accumulation buffer
				ProcessCompletePackets();

				// Continue reading
				_stream.BeginRead(_buffer, 0, _buffer.Length, ReceiveCallback, null);
			} catch (Exception e) {
				Logger.LogError($"Error in ReceiveCallback: {e.Message}");
				Close().Forget();
			}
		}

		private void ProcessCompletePackets() {
			_receiveBuffer.Goto(0);
			
			while (_receiveBuffer.length >= 2) {
				var startPos = _receiveBuffer.offset;
				
				// Read packet length (first 2 bytes)
				var packetLength = _receiveBuffer.ReadUShort();
				
				// Check if we have the complete packet
				if (_receiveBuffer.length < startPos + packetLength) {
					// Not enough data for complete packet, wait for more
					_receiveBuffer.Goto(startPos);
					break;
				}
				
				// Extract complete packet
				var packetData = new Buffer();
				packetData.Write(packetLength);
				for (var i = 0; i < packetLength - 2; i++) {
					packetData.Write(_receiveBuffer.ReadByte());
				}
				
				packetData.Goto(0);
				OnReceivedEvent?.Invoke(packetData);
			}
			
			// Remove processed data from accumulation buffer
			if (_receiveBuffer.offset > 0) {
				var remainingData = new byte[_receiveBuffer.length - _receiveBuffer.offset];
				Array.Copy(_receiveBuffer.data, _receiveBuffer.offset, remainingData, 0, remainingData.Length);
				_receiveBuffer = new Buffer();
				_receiveBuffer.Write(remainingData);
			}
		}

		public async UniTask<bool> Send(Buffer buffer) {
			try {
				if (!IsConnected()) {
					Logger.LogWarning("Cannot send: not connected");
					return false;
				}
				
				await _stream.WriteAsync(buffer.data, 0, buffer.length);
				return true;
			} catch (Exception e) {
				Logger.LogError("Error sending data to server: " + e.Message);
				await Close();
				return false;
			}
		}

		public UniTask Close() {
			try {
				_stream?.Close();
				_client?.Close();
			} catch (Exception e) {
				Logger.LogWarning($"Error closing connection: {e.Message}");
			} finally {
				_stream = null;
				_client = null;
				_receiveBuffer?.Clear();
			}
			return UniTask.CompletedTask;
		}
	}
}