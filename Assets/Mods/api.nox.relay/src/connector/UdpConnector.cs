using System.Net;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.connector
{
    public class UdpConnector : IConnector
    {
        private UdpClient _client;
        private IPEndPoint _endPoint;

        public string Type => "UDP";

        public UniTask<bool> Connect(string address, ushort port)
        {
            _client = new UdpClient();
            _endPoint = new IPEndPoint(IPAddress.Parse(address), port);
            return UniTask.FromResult(true);
        }

        public async UniTask<bool> Send(Buffer buffer)
        {
            try
            {
                await _client.SendAsync(buffer.data, buffer.length, _endPoint);
                return true;
            }
            catch (SocketException e)
            {
                Logger.LogError($"Failed to send data ({e.Message})");
                await Close();
            }

            return false;
        }

        public bool IsConnected() => _client != null;

        public void Update()
        {
            if (_client == null) return;
            try
            {
                if (_client.Available <= 0) return;
                var data = _client.Receive(ref _endPoint);
                var buffer = new Buffer();
                buffer.Write(data);
                buffer.Goto(0);
                OnReceivedEvent?.Invoke(buffer);
            }
            catch (SocketException e)
            {
                Logger.LogError($"Failed to receive data ({e.Message})");
                Close();
            }
        }

        public UniTask Close()
        {
            _client?.Close();
            _client = null;
            _endPoint = null;
            return UniTask.CompletedTask;
        }

        public IPEndPoint Remote() => _endPoint;
        public event IConnector.OnReceived OnReceivedEvent;
    }
}