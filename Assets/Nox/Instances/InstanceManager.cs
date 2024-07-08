using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.Scripts;
using Nox.Servers;
using UnityEngine;
using UnityEngine.Networking;

namespace Nox.Instances
{
    public class InstanceManager : Manager<Instance>
    {
        public static Instance Get(uint id, string serverAddress)
            => Cache.Find(i => i.id == id && i.server == serverAddress);

        public static async UniTask<Instance> GetOrFetch(uint id, string serverAddress)
            => Get(id, serverAddress) ?? await Fetch(id, serverAddress);

        public static async UniTask<Instance> Fetch(uint id, string serverAddress, bool withEmpty = false)
        {
            var server = await ServerManager.GetOrFetch(serverAddress);
            if (server == null) return null;
            var req = new UnityWebRequest(server.gateways.CombineHTTP($"/api/instances/{id}{(withEmpty ? "?empty" : "")}"), "GET")
            { downloadHandler = new DownloadHandlerBuffer() };
            req.SetRequestHeader("Authorization", Lookup.MostAuth(serverAddress));
            try { await req.SendWebRequest(); }
            catch { return null; }
            if (req.responseCode != 200) return null;
            var res = JsonUtility.FromJson<Response<Instance>>(req.downloadHandler.text);
            if (res.IsError) return null;
            return Set(res.data);
        }
    }
}