using System;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.Scripts;
using Nox.Servers;
using UnityEngine;
using UnityEngine.Networking;

namespace Nox.Worlds
{
    public class WorldManager : Manager<World>
    {
        public static World Get(uint id, string server) => Cache.Find(w => w.id == id && w.server == server);
        public static World Get(string worldRef)
        {
            var patern = new WorldPatern(worldRef);
            return Get(patern.id, patern.server);
        }
        public static async UniTask<World> GetOrFetch(uint id, string server) => Get(id, server) ?? await Fetch(id, server);
        public static async UniTask<World> GetOrFetch(string worldRef, string defaultServer = null)
        {
            var patern = new WorldPatern(worldRef, defaultServer);
            return await GetOrFetch(patern.id, patern.server);
        }

        public static async UniTask<World> Fetch(uint id, string server)
        {
            Debug.Log($"Fetching world {id} from {server}");
            var gateway = (await ServerManager.GetOrFetch(server))?.gateways;
            if (gateway == null) return null;
            var req = new UnityWebRequest(gateway.CombineHTTP($"/api/worlds/{id}"), "GET")
            { downloadHandler = new DownloadHandlerBuffer() };
            req.SetRequestHeader("Authorization", Lookup.MostAuth(server));
            try { await req.SendWebRequest(); }
            catch { return null; }
            if (req.responseCode != 200) return null;
            var res = JsonUtility.FromJson<Response<World>>(req.downloadHandler.text);
            if (res.IsError) return null;
            return Set(res.data);
        }
        public static async UniTask<World> Fetch(string worldRef)
        {
            var patern = new WorldPatern(worldRef);
            return await Fetch(patern.id, patern.server);
        }

        internal static async UniTask<bool> DownloadAsset(WorldAsset worldAsset)
        {
            var hash = worldAsset.hash;
            if (CCK.Cache.Has(hash)) return true;
            var url = worldAsset.url;
            var random_id = Guid.NewGuid().ToString();
            var cache_path = CCK.Cache.GetPath(random_id);
            var handler = new DownloadHandlerFile(cache_path) { removeFileOnAbort = true };
            var req = new UnityWebRequest(url, "GET") { downloadHandler = handler };
            try { await req.SendWebRequest(); }
            catch
            {
                CCK.Cache.Delete(random_id);
                return false;
            }
            if (req.responseCode != 200 || !Hashing.HashFile(cache_path).Equals(hash))
            {
                CCK.Cache.Delete(random_id);
                return false;
            };
            CCK.Cache.Move(random_id, hash);
            return true;
        }
    }
}