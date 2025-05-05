using System;
using System.Net;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.connector
{
    public class TcpConnector : IConnector
    {
        // TCP client variables
        private TcpClient _client = null;
        private NetworkStream _stream = null;
        private byte[] _buffer = new byte[1024];

        public string Type => "TCP";

        public IPEndPoint Remote() => _client.Client.RemoteEndPoint as IPEndPoint;
        public event IConnector.OnReceived OnReceivedEvent;

        public bool IsConnected() => _client is { Connected: true };

        public UniTask<bool> Connect(string address, ushort port)
        {
            try
            {
                _client = new TcpClient(address, port);
                _stream = _client.GetStream();
                _stream.BeginRead(_buffer, 0, _buffer.Length, ReceiveCallback, null);
                return UniTask.FromResult(true);
            }
            catch (SocketException e)
            {
                Logger.LogError($"Failed to connect to {address}:{port} ({e.Message})");
                return UniTask.FromResult(false);
            }
        }

        public void Update()
        {
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            if (!IsConnected()) return;
            var bytesRead = _stream.EndRead(result);
            var buffer = new Buffer();
            for (var i = 0; i < bytesRead; i++)
                buffer.Write(_buffer[i]);
            buffer.Goto(0);
            OnReceivedEvent?.Invoke(buffer);
            _stream.BeginRead(_buffer, 0, _buffer.Length, ReceiveCallback, null);
        }

        public async UniTask<bool> Send(Buffer buffer)
        {
            try
            {
                _stream.Write(buffer.data, 0, buffer.length);
                return true;
            }
            catch (Exception e)
            {
                Logger.LogError("Error sending data to server: " + e.Message);
                await Close();
            }

            return false;
        }

        public UniTask Close()
        {
            _stream?.Close();
            _client?.Close();
            _stream = null;
            _client = null;
            return UniTask.CompletedTask;
        }
    }
}