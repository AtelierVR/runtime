
using System;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Mods.Networks;
using Nox.CCK.Servers;
using UnityEngine;
using UnityEngine.Networking;

namespace api.nox.network
{
    public class NetServer : NetworkAPIServer
    {
        private readonly NetworkSystem _mod;

        internal Server server;

        internal NetServer(NetworkSystem mod) => _mod = mod;

        public async UniTask<Server> GetMyServer()
        {
            var config = Config.Load();
            if (!config.Has("token") || !config.Has("gateway"))
            {
                server = null;
                _mod._api.EventAPI.Emit(new NetEventContext("network.server", server, true));
                return null;
            }
            var req = new UnityWebRequest(string.Format("{0}/api/server", config.Get<string>("gateway")), "GET") { downloadHandler = new DownloadHandlerBuffer() };
            req.SetRequestHeader("Authorization", string.Format("Bearer {0}", config.Get<string>("token")));
            try { await req.SendWebRequest(); }
            catch { }
            if (req.responseCode != 200)
            {
                server = null;
                _mod._api.EventAPI.Emit(new NetEventContext("network.server", server, true));
                return null;
            }
            var response = JsonUtility.FromJson<Response<Server>>(req.downloadHandler.text);
            server = response.data;
            _mod._api.EventAPI.Emit(new NetEventContext("network.server", server, true));
            return response.data;
        }

        public async UniTask<Server> GetServer(string address)
        {
            Uri gateway = await Gateway.FindGatewayMaster(address);
            if (gateway == null) return null;
            var req = new UnityWebRequest(string.Format("{0}/api/server", gateway), "GET") { downloadHandler = new DownloadHandlerBuffer() };
            try { await req.SendWebRequest(); }
            catch { }
            if (req.responseCode != 200) return null;
            var response = JsonUtility.FromJson<Response<Server>>(req.downloadHandler.text);
            return response.data;
        }

        public async UniTask<WellKnownServer> GetWellKnown(string address)
        {
            Uri gateway = await Gateway.FindGatewayMaster(address);
            if (gateway == null) return null;
            var req = new UnityWebRequest(string.Format("{0}/.well-known/nox", gateway), "GET") { downloadHandler = new DownloadHandlerBuffer() };
            try { await req.SendWebRequest(); }
            catch { }
            if (req.responseCode != 200) return null;
            var response = JsonUtility.FromJson<Response<WellKnownServer>>(req.downloadHandler.text);
            return response.data;
        }
    }
}