using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Mods;
using UnityEngine;
using UnityEngine.Networking;

namespace api.nox.network
{
    public class NetInstance : ShareObject
    {
        private readonly NetworkSystem _mod;

        internal Server server;

        internal NetInstance(NetworkSystem mod) => _mod = mod;

        public async UniTask<Instance> GetInstance(string server, uint instanceId)
        {
            var User = _mod.GetCurrentUser();
            if (User == null) return null;
            Debug.Log("Getting instance");
            var config = Config.Load();
            var gateway = server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null) return null;
            var req = new UnityWebRequest($"{gateway}/api/instances/{instanceId}", "GET") { downloadHandler = new DownloadHandlerBuffer() };
            req.SetRequestHeader("Authorization", _mod.MostAuth(server));
            try { await req.SendWebRequest(); }
            catch { return null; }
            if (req.responseCode != 200) return null;
            var res = JsonUtility.FromJson<Response<Instance>>(req.downloadHandler.text);
            if (res.IsError) return null;
            Debug.Log(req.downloadHandler.text);
            return res.data;
        }

        public async UniTask<InstanceSearch> SearchInstances(string server, string query, uint offset = 0, uint limit = 10)
        {
            // GET /api/instances/search?query={query}&offset={offset}&limit={limit}
            var User = _mod.GetCurrentUser();
            var config = Config.Load();
            var gateway = server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null) return null;
            var req = new UnityWebRequest($"{gateway}/api/instances/search?query={query}&offset={offset}&limit={limit}", "GET") { downloadHandler = new DownloadHandlerBuffer() };
            req.SetRequestHeader("Authorization", _mod.MostAuth(server));
            try { await req.SendWebRequest(); }
            catch { return null; }
            Debug.Log(req.downloadHandler.text);
            if (req.responseCode != 200) return null;
            var res = JsonUtility.FromJson<Response<InstanceSearch>>(req.downloadHandler.text);
            if (res.IsError) return null;
            return res.data;
        }
    }
}
