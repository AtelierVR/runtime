
using System;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Mods;
using UnityEngine;
using UnityEngine.Networking;

namespace api.nox.network
{
    public class ServerAPI : ShareObject
    {
        private readonly NetworkSystem _mod;

        internal Server server;

        internal ServerAPI(NetworkSystem mod) => _mod = mod;

        private async UniTask<Server> GetMyServer()
        {
            var config = Config.Load();
            if (!config.Has("token") || !config.Has("gateway")) return null;
            var req = new UnityWebRequest(string.Format("{0}/api/server", config.Get<string>("gateway")), "GET") { downloadHandler = new DownloadHandlerBuffer() };
            req.SetRequestHeader("Authorization", string.Format("Bearer {0}", config.Get<string>("token")));
            try { await req.SendWebRequest(); }
            catch { }
            if (req.responseCode != 200) return null;
            var response = JsonUtility.FromJson<Response<Server>>(req.downloadHandler.text);
            if (response.IsError) return null;
            server = response.data;
            _mod._api.EventAPI.Emit(new NetEventContext("network.get.server.me", server, true));
            _mod._api.EventAPI.Emit(new NetEventContext("network.get.server", server));
            return response.data;
        }

        private async UniTask<Server> GetServer(string address)
        {
            Uri gateway = await Gateway.FindGatewayMaster(address);
            if (gateway == null) return null;
            var req = new UnityWebRequest(string.Format("{0}/api/server", gateway), "GET") { downloadHandler = new DownloadHandlerBuffer() };
            try { await req.SendWebRequest(); }
            catch { }
            if (req.responseCode != 200) return null;
            var response = JsonUtility.FromJson<Response<Server>>(req.downloadHandler.text);
            if (response.IsError) return null;
            _mod._api.EventAPI.Emit(new NetEventContext("network.get.server", address, response.data));
            return response.data;
        }

        private async UniTask<WellKnownServer> GetWellKnown(string address)
        {
            Uri gateway = await Gateway.FindGatewayMaster(address);
            if (gateway == null) return null;
            var req = new UnityWebRequest(string.Format("{0}/.well-known/nox", gateway), "GET") { downloadHandler = new DownloadHandlerBuffer() };
            try { await req.SendWebRequest(); }
            catch { }
            if (req.responseCode != 200) return null;
            var response = JsonUtility.FromJson<Response<WellKnownServer>>(req.downloadHandler.text);
            if (response.IsError) return null;
            _mod._api.EventAPI.Emit(new NetEventContext("network.get.wellknown", address, response.data));
            return response.data;
        }
        private async UniTask<ServerSearch> SearchServers(string server, string query, uint offset = 0, uint limit = 10)
        {
            return new ServerSearch() { servers = new Server[0], total = 0, limit = limit, offset = offset };
            // GET /api/servers/search?query={query}&offset={offset}&limit={limit}
            // var User = _mod._api.NetworkAPI.GetCurrentUser();
            // var config = Config.Load();
            // var gateway = server == User.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            // if (gateway == null) return null;
            // var req = new UnityWebRequest($"{gateway}/api/servers/search?query={query}&offset={offset}&limit={limit}", "GET") { downloadHandler = new DownloadHandlerBuffer() };
            // if (_mod.TryMostAuth(server, out var auth)) req.SetRequestHeader("Authorization", auth);
            // try { await req.SendWebRequest(); }
            // catch { return null; }
            // if (req.responseCode != 200) return null;
            // var response = JsonUtility.FromJson<Response<ServerSearch>>(req.downloadHandler.text);
            // return response.data;
        }

        
        [ShareObjectExport] public Func<string, string, uint, uint, UniTask<ShareObject>> SharedSearchServers;
        [ShareObjectExport] public Func<UniTask<ShareObject>> SharedGetMyServer;
        [ShareObjectExport] public Func<string, UniTask<ShareObject>> SharedGetServer;
        [ShareObjectExport] public Func<string, UniTask<ShareObject>> SharedGetWellKnown;

        public void BeforeExport()
        {
            SharedSearchServers = async (server, query, offset, limit) =>await  SearchServers(server, query, offset, limit);
            SharedGetMyServer = async () => await GetMyServer();
            SharedGetServer = async (address) => await GetServer(address);
            SharedGetWellKnown = async (address) => await GetWellKnown(address);
        }

        public void AfterExport()
        {
            SharedGetMyServer = null;
            SharedSearchServers = null;
            SharedGetServer = null;
            SharedGetWellKnown = null;
        }
    }
}