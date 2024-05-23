using System;
using System.Net;
using System.Net.Sockets;
using Buffer = Nox.Scripts.Buffer;
using Random = UnityEngine.Random;

namespace Nox.Network
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class TcpConnector : IConnector
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        // TCP client variables
        private TcpClient _client = null;
        private NetworkStream _stream = null;
        private byte[] _buffer = new byte[1024];

        public string Type => "TCP";

        public IPEndPoint Remote() => _client.Client.RemoteEndPoint as IPEndPoint;
        public event IConnector.OnReceived OnReceivedEvent;

        public bool IsConnected() => _client is { Connected: true };

        public bool Connect(string address, ushort port)
        {
            try
            {
                _client = new TcpClient(address, port);
                _stream = _client.GetStream();
                _stream.BeginRead(_buffer, 0, _buffer.Length, ReceiveCallback, null);
                return true;
            }
            catch (SocketException e)
            {
                Debug.LogError($"Failed to connect to {address}:{port} ({e.Message})");
                return false;
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

        public bool Send(Buffer buffer)
        {
            try
            {
                _stream.Write(buffer.data, 0, buffer.length);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("Error sending data to server: " + e.Message);
                Close();
            }

            return false;
        }

        public void Close()
        {
            _stream?.Close();
            _client?.Close();
            _stream = null;
            _client = null;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            var other = (TcpConnector) obj;
            return other.Remote().Address == Remote().Address && other.Remote().Port == Remote().Port;
        }
    }
}