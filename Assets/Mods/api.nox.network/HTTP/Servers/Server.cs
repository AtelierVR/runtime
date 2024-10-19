using System;
using api.nox.network.HTTP;
using api.nox.network.WebSockets;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace api.nox.network.Servers
{
    [Serializable]
    public class Server : ICached
    {
        public string id;
        public string title;
        public string description;
        public string address;
        public string version;
        public ulong ready_at;
        public string icon;
        public string public_key;
        public ServerGateway gateways;
        public string[] features;

        public async UniTask<Auths.AuthToken> GetToken() => await NetworkSystem.ModInstance.Auth.GetToken(address);

        public async UniTask<WebSocket> GetOrConnect()
        {
            if (gateways.ws == null) return null;
            var socket = NetworkSystem.ModInstance.WebSocket.GetWebSocket(address);
            if (socket == null)
            {
                var token = await GetToken();
                if (token == null) return null;
                socket = NetworkSystem.ModInstance.WebSocket.CreateWebSocket(address, null);
                var ws = new System.Net.WebSockets.ClientWebSocket();
                ws.Options.SetRequestHeader("Authorization", token.ToHeader());
                var result = await socket.Connect(gateways.ws, ws);
                if (!result)
                {
                    Debug.LogError($"Failed to connect to {gateways.ws}");
                    socket.Dispose();
                    return null;
                }
            }
            return socket;
        }

        public string GetCacheKey() => $"server.{address}";
    }
}