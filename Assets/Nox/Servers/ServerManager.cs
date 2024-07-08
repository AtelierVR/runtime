using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Users;
using Nox.Scripts;
using Nox.Users;
using UnityEngine;
using UnityEngine.Networking;

namespace Nox.Servers
{
    public class ServerManager : Manager<Server>
    {
        public static Server Get(string address) => Cache.Find(s => s.address == address);
        public static async UniTask<Server> GetOrFetch(string address) => Get(address) ?? await Fetch(address);

        public static async UniTask<Server> Fetch(string address)
        {
            var gateway = Cache.Find(s => s.address == address)?.gateways.http ?? (await Gateway.FindGatewayMaster(address))?.OriginalString;
            if (gateway == null) return null;
            var req = new UnityWebRequest($"{gateway}/api/server", "GET")
            { downloadHandler = new DownloadHandlerBuffer() };
            req.SetRequestHeader("Authorization", Lookup.MostAuth(address));
            try { await req.SendWebRequest(); }
            catch { return null; }
            if (req.responseCode != 200) return null;
            var res = JsonUtility.FromJson<Response<Server>>(req.downloadHandler.text);
            if (res.IsError) return null;
            return Set(res.data);
        }
    }
}