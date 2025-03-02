using System;
using api.nox.network.WebSockets;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.network.Servers
{
    [Serializable]
    public class Server : ICached, INoxObject
    {
        [NoxPublic(NoxAccess.Read)] public string id;
        [NoxPublic(NoxAccess.Read)] public string title;
        [NoxPublic(NoxAccess.Read)] public string description;
        [NoxPublic(NoxAccess.Read)] public string address;
        [NoxPublic(NoxAccess.Read)] public string version;
        [NoxPublic(NoxAccess.Read)] public ulong ready_at;
        [NoxPublic(NoxAccess.Read)] public string icon;
        [NoxPublic(NoxAccess.Read)] public string public_key;
        [NoxPublic(NoxAccess.Read)] public ServerGateway gateways;
        [NoxPublic(NoxAccess.Read)] public string[] features;

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<Auths.AuthToken> GetToken() =>
            await NetworkSystem.ModInstance.Auth.GetToken(address);

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<WebSocket> GetOrConnect()
        {
            if (gateways.ws == null)
                return null;
            var socket = NetworkSystem.ModInstance.WebSocket.GetWebSocket(address);
            if (socket == null)
            {
                var token = await GetToken();
                if (token == null) return null;
                socket = NetworkSystem.ModInstance.WebSocket.CreateWebSocket(address, gateways.ws);
                var ws = new System.Net.WebSockets.ClientWebSocket();
                ws.Options.SetRequestHeader("Authorization", token.ToHeader());
                var result = await socket.Connect(null, ws);
                if (!result)
                {
                    Logger.LogError($"Failed to connect to {gateways.ws}");
                    socket.Dispose();
                    return null;
                }
            }

            return socket;
        }

        public static string GetCacheKey(string address)
            => $"server.{address}";

        public string GetCacheKey() => GetCacheKey(address);
        public override string ToString() => $"{GetType().Name}[address={address}, title={title}, version={version}]";
    }
}