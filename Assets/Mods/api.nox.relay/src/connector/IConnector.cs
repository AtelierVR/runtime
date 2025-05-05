using System.Net;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace api.nox.relay.connector
{
    public interface IConnector
    {
        string Type { get; }

        bool IsConnected();
        IPEndPoint Remote();
        UniTask<bool> Connect(string address, ushort port);
        UniTask Close();

        event OnReceived OnReceivedEvent;

        delegate void OnReceived(Buffer buffer);

        UniTask<bool> Send(Buffer buffer);
        void Update();
    }
}