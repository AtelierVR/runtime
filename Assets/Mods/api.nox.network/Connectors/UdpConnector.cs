using System.Net;
using System.Net.Sockets;
using api.nox.network.Utils;
using UnityEngine;

namespace api.nox.network
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class UdpConnector : IConnector
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        private UdpClient _client;
        private IPEndPoint _endPoint;

        public string Type => "UDP";

        public bool Connect(string address, ushort port)
        {
            _client = new UdpClient();
            _endPoint = new IPEndPoint(IPAddress.Parse(address), port);
            return true;
        }

        public bool Send(Buffer buffer)
        {
            try
            {
                _client.Send(buffer.data, buffer.length, _endPoint);
                return true;
            }
            catch (SocketException e)
            {
                Debug.LogError($"Failed to send data ({e.Message})");
                Close();
            }

            return false;
        }

        public bool IsConnected() => _client != null;

        public void Update()
        {
            if (_client == null) return;
            try
            {
                if (_client.Available <= 0)
                    return;
                var data = _client.Receive(ref _endPoint);
                var buffer = new Buffer();
                foreach (var t in data) buffer.Write(t);
                buffer.Goto(0);
                OnReceivedEvent?.Invoke(buffer);
            }
            catch (SocketException e)
            {
                Debug.LogError($"Failed to receive data ({e.Message})");
                Close();
            }
        }

        public void Close()
        {
            _client?.Close();
            _client = null;
        }

        public IPEndPoint Remote() => _endPoint;
        public event IConnector.OnReceived OnReceivedEvent;

        
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            var other = (UdpConnector) obj;
            return other.Remote().Address == Remote().Address && other.Remote().Port == Remote().Port;
        }
    }
}