using System;
using Cysharp.Threading.Tasks;
using WS = System.Net.WebSockets.ClientWebSocket;
using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class Server : ShareObject
    {
        internal NetworkSystem _mod;
        [ShareObjectExport] public string id;
        [ShareObjectExport] public string title;
        [ShareObjectExport] public string description;
        [ShareObjectExport] public string address;
        [ShareObjectExport] public string version;
        [ShareObjectExport] public long ready_at;
        [ShareObjectExport] public string icon;
        [ShareObjectExport] public string public_key;
        [ShareObjectExport] public ServerGateway gateways;
        [ShareObjectExport] public string[] features;

        [ShareObjectExport] public Func<UniTask<ShareObject>> SharedGetToken;
        [ShareObjectExport] public Func<UniTask<ShareObject>> SharedGetOrConnect;


        private async UniTask<AuthToken> GetToken() => await _mod._auth.GetToken(address);
        private async UniTask<WebSocket> GetOrConnect()
        {
            if (gateways.ws == null) return null;
            var socket = _mod._ws.GetWebSocket(address);
            if (socket == null)
            {
                var token = await GetToken();
                if (token == null) return null;
                socket = _mod._ws.CreateWebSocket(address, null);
                var ws = new WS();
                ws.Options.SetRequestHeader("Authorization", token.ToHeader());
                var result = await socket.Connect(gateways.ws, ws);
                if (!result)
                {
                    socket.Dispose();
                    return null;
                }
            }
            return socket;
        }

        public void BeforeExport()
        {
            SharedGetToken = async () => await GetToken();
            SharedGetOrConnect = async () => await GetOrConnect();
        }
    }
}