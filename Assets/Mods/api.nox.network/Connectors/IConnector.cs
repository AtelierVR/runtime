using System.Net;
using api.nox.network.Utils;

namespace api.nox.network
{
    public interface IConnector
    {
        string Type { get; }
        bool IsConnected();
        bool Connect(string address, ushort port);
        void Close();
        bool Send(Buffer buffer);
        IPEndPoint Remote();
        
        delegate void OnReceived(Buffer buffer);
        event OnReceived OnReceivedEvent;
        void Update();
    }
}