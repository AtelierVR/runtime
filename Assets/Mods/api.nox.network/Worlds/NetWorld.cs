using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Mods;
using UnityEngine;
using UnityEngine.Networking;

namespace api.nox.network
{
    public class NetWorld : ShareObject
    {
        private readonly NetworkSystem _mod;
        internal NetWorld(NetworkSystem mod) => _mod = mod;

        public async UniTask<ShareObject> GetAsset(string server, uint worldId, uint assetId) => await GetIAsset(server, worldId, assetId);
        private async UniTask<WorldAsset> GetIAsset(string server, uint worldId, uint assetId)
        {
            var User = _mod.GetCurrentUser();
            if (User == null) return null;
            Debug.Log("Getting asset");
            var config = Config.Load();
            var gateway = server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null) return null;
            var req = new UnityWebRequest($"{gateway}/api/worlds/{worldId}/assets/{assetId}", "GET") { downloadHandler = new DownloadHandlerBuffer() };
            if (_mod.TryMostAuth(server, out var auth)) req.SetRequestHeader("Authorization", auth);
            try { await req.SendWebRequest(); }
            catch { return null; }
            if (req.responseCode != 200) return null;
            Debug.Log(req.downloadHandler.text);
            var res = JsonUtility.FromJson<Response<WorldAsset>>(req.downloadHandler.text);
            if (res.IsError) return null;
            return res.data;
        }

        // public async UniTask<ShareObject> UpdateAsset(UpdateAssetData asset) => await UpdateIAsset(asset);
        // private async UniTask<WorldAsset> UpdateIAsset(UpdateAssetData asset)
        // {
        //     var User = _mod._api.NetworkAPI.CallMethod<User>("GetCurrentUser");
        //     if (User == null) return null;
        //     Debug.Log("Updating asset");
        //     var config = Config.Load();
        //     var gateway = asset.server == User.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(asset.server))?.OriginalString;
        //     if (gateway == null) return null;
        //     var req = new UnityWebRequest($"{gateway}/api/worlds/{asset.worldId}/assets/{asset.id}", "POST") { downloadHandler = new DownloadHandlerBuffer() };
        //     req.SetRequestHeader("Authorization", _mod.MostAuth(asset.server));
        //     req.SetRequestHeader("Content-Type", "application/json");
        //     req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(asset)));
        //     try { await req.SendWebRequest(); }
        //     catch { return null; }
        //     if (req.responseCode != 200) return null;
        //     Debug.Log(req.downloadHandler.text);
        //     var res = JsonUtility.FromJson<Response<WorldAsset>>(req.downloadHandler.text);
        //     if (res.IsError) return null;
        //     return res.data;
        // }

        public async UniTask<bool> DeleteAsset(string server, uint worldId, uint assetId)
        {
            var User = _mod.GetCurrentUser();
            if (User == null) return false;
            Debug.Log("Deleting asset");
            var config = Config.Load();
            var gateway = server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null) return false;
            var req = new UnityWebRequest($"{gateway}/api/worlds/{worldId}/assets/{assetId}", "DELETE") { downloadHandler = new DownloadHandlerBuffer() };
            if (_mod.TryMostAuth(server, out var auth)) req.SetRequestHeader("Authorization", auth);
            try { await req.SendWebRequest(); }
            catch { return false; }
            if (req.responseCode != 200) return false;
            return true;
        }

        public async UniTask<bool> DeleteWorld(string server, uint worldId)
        {
            var User = _mod.GetCurrentUser();
            if (User == null) return false;
            Debug.Log("Deleting world");
            var config = Config.Load();
            var gateway = server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null) return false;
            var req = new UnityWebRequest($"{gateway}/api/worlds/{worldId}", "DELETE") { downloadHandler = new DownloadHandlerBuffer() };
            if (_mod.TryMostAuth(server, out var auth)) req.SetRequestHeader("Authorization", auth);
            try { await req.SendWebRequest(); }
            catch { return false; }
            if (req.responseCode != 200) return false;
            return true;
        }

        public async UniTask<bool> UploadAssetFile(string server, uint worldId, uint assetId, string path)
        {
            var User = _mod.GetCurrentUser();
            if (User == null) return false;
            Debug.Log("Uploading asset file");
            var config = Config.Load();
            var gateway = server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null) return false;
            byte[] fileBytes = await ReadFileAsync(path);
            var form = new WWWForm();
            form.AddBinaryData("file", fileBytes, Path.GetFileName(path));
            var req = UnityWebRequest.Post($"{gateway}/api/worlds/{worldId}/assets/{assetId}/file", form);
            if (_mod.TryMostAuth(server, out var auth)) req.SetRequestHeader("Authorization", auth);
            req.SetRequestHeader("X-File-Hash", Hashing.HashFile(path));
            try { await req.SendWebRequest(); }
            catch
            {
                Debug.Log(req.downloadHandler.text);
                return false;
            }
            if (req.responseCode != 200) return false;
            return true;
        }

        public async UniTask<bool> UploadWorldThumbnail(string server, uint worldId, string path)
        {
            var User = _mod.GetCurrentUser();
            if (User == null) return false;
            Debug.Log("Uploading world thumbnail");
            var config = Config.Load();
            var gateway = server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null) return false;
            byte[] fileBytes = await ReadFileAsync(path);
            var form = new WWWForm();
            form.AddBinaryData("file", fileBytes, Path.GetFileName(path));
            var req = UnityWebRequest.Post($"{gateway}/api/worlds/{worldId}/thumbnail", form);
            if (_mod.TryMostAuth(server, out var auth)) req.SetRequestHeader("Authorization", auth);
            req.SetRequestHeader("X-File-Hash", Hashing.HashFile(path));
            try { await req.SendWebRequest(); }
            catch { return false; }
            if (req.responseCode != 200) return false;
            return true;
        }


        // public async UniTask<ShareObject> CreateAsset(CreateAssetData asset) => await CreateIAsset(asset);
        // private async UniTask<WorldAsset> CreateIAsset(CreateAssetData asset)
        // {
        //     var User = _mod._api.NetworkAPI.CallMethod<User>("GetCurrentUser");
        //     if (User == null) return null;
        //     Debug.Log("Creating asset");
        //     var config = Config.Load();
        //     var gateway = asset.server == User.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(asset.server))?.OriginalString;
        //     if (gateway == null) return null;
        //     var req = new UnityWebRequest($"{gateway}/api/worlds/{asset.worldId}/assets", "PUT") { downloadHandler = new DownloadHandlerBuffer() };
        //     req.SetRequestHeader("Authorization", _mod.MostAuth(asset.server));
        //     req.SetRequestHeader("Content-Type", "application/json");
        //     req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(asset)));
        //     try { await req.SendWebRequest(); }
        //     catch { return null; }
        //     Debug.Log(req.downloadHandler.text);
        //     if (req.responseCode != 200) return null;
        //     var res = JsonUtility.FromJson<Response<WorldAsset>>(req.downloadHandler.text);
        //     if (res.IsError) return null;
        //     return res.data;
        // }

        // public async UniTask<ShareObject> CreateWorld(CreateWorldData world) => await CreateIWorld(world);
        // private async UniTask<World> CreateIWorld(CreateWorldData world)
        // {
        //     var User = _mod._api.NetworkAPI.CallMethod<User>("GetCurrentUser");
        //     if (User == null) return null;
        //     Debug.Log("Creating world");
        //     var config = Config.Load();
        //     var gateway = world.server == User.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(world.server))?.OriginalString;
        //     if (gateway == null) return null;
        //     var req = new UnityWebRequest($"{gateway}/api/worlds", "PUT") { downloadHandler = new DownloadHandlerBuffer() };
        //     req.SetRequestHeader("Authorization", _mod.MostAuth(world.server));
        //     req.SetRequestHeader("Content-Type", "application/json");
        //     req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(world)));
        //     try { await req.SendWebRequest(); }
        //     catch { return null; }
        //     Debug.Log(req.downloadHandler.text);
        //     if (req.responseCode != 200) return null;
        //     Debug.Log(req.downloadHandler.text);
        //     var res = JsonUtility.FromJson<Response<World>>(req.downloadHandler.text);
        //     if (res.IsError) return null;
        //     return res.data;
        // }

        public async UniTask<ShareObject> GetWorld(string server, uint worldId) => await GetIWorld(server, worldId);
        private async UniTask<World> GetIWorld(string server, uint worldId, bool withEmpty = false)
        {
            var User = _mod.GetCurrentUser();
            if (User == null) return null;
            Debug.Log("Getting world");
            var config = Config.Load();
            var gateway = server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null) return null;
            var req = new UnityWebRequest($"{gateway}/api/worlds/{worldId}{(withEmpty ? "?empty" : "")}", "GET") { downloadHandler = new DownloadHandlerBuffer() };
            if (_mod.TryMostAuth(server, out var auth)) req.SetRequestHeader("Authorization", auth);
            try { await req.SendWebRequest(); }
            catch { return null; }
            if (req.responseCode != 200) return null;
            var res = JsonUtility.FromJson<Response<World>>(req.downloadHandler.text);
            if (res.IsError) return null;
            Debug.Log(req.downloadHandler.text);
            return res.data;
        }

        // public async UniTask<ShareObject> UpdateWorld(UpdateWorldData world, bool withEmpty = false) => await UpdateIWorld(world, withEmpty);
        // private async UniTask<World> UpdateIWorld(UpdateWorldData world, bool withEmpty = false)
        // {
        //     var User = _mod._api.NetworkAPI.CallMethod<User>("GetCurrentUser");
        //     if (User == null) return null;
        //     Debug.Log("Updating world");
        //     var config = Config.Load();
        //     var gateway = world.server == User.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(world.server))?.OriginalString;
        //     if (gateway == null) return null;
        //     var req = new UnityWebRequest($"{gateway}/api/worlds/{world.id}{(withEmpty ? "?empty" : "")}", "POST") { downloadHandler = new DownloadHandlerBuffer() };
        //     req.SetRequestHeader("Authorization", _mod.MostAuth(world.server));
        //     req.SetRequestHeader("Content-Type", "application/json");
        //     req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(world)));
        //     try { await req.SendWebRequest(); }
        //     catch { return null; }
        //     if (req.responseCode != 200) return null;
        //     var res = JsonUtility.FromJson<Response<World>>(req.downloadHandler.text);
        //     if (res.IsError) return null;
        //     return res.data;
        // }

        async UniTask<byte[]> ReadFileAsync(string path)
        {
            using FileStream sourceStream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            byte[] buffer = new byte[sourceStream.Length];
            await sourceStream.ReadAsync(buffer, 0, (int)sourceStream.Length);
            return buffer;
        }

        private async UniTask<WorldSearch> SearchIWorlds(string server, string query, uint offset = 0, uint limit = 10)
        {
            // GET /api/worlds/search?query={query}&offset={offset}&limit={limit}
            var User = _mod.GetCurrentUser();
            var config = Config.Load();
            Debug.Log("Searching worlds");
            Debug.Log("User: " + User);
            Debug.Log("Args: " + server + " " + query + " " + offset + " " + limit);
            var gateway = server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null) return null;
            var req = new UnityWebRequest($"{gateway}/api/worlds/search?query={query}&offset={offset}&limit={limit}", "GET") { downloadHandler = new DownloadHandlerBuffer() };
            if (_mod.TryMostAuth(server, out var auth)) req.SetRequestHeader("Authorization", auth);
            try { await req.SendWebRequest(); }
            catch { return null; }
            if (req.responseCode != 200) return null;
            var res = JsonUtility.FromJson<Response<WorldSearch>>(req.downloadHandler.text);
            if (res.IsError) return null;
            return res.data;
        }

        private async UniTask<World[]> GetIWorlds(string server, uint[] worldIds)
        {
            var User = _mod.GetCurrentUser();
            if (User == null) return new World[0];
            Debug.Log("Getting worlds");
            var config = Config.Load();
            var gateway = server == User.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null) return new World[0];
            var req = new UnityWebRequest($"{gateway}/api/worlds/search?id={string.Join("&id=", worldIds)}", "GET") { downloadHandler = new DownloadHandlerBuffer() };
            if (_mod.TryMostAuth(server, out var auth)) req.SetRequestHeader("Authorization", auth);
            try { await req.SendWebRequest(); }
            catch { return new World[0]; }
            if (req.responseCode != 200) return new World[0];
            var res = JsonUtility.FromJson<Response<WorldSearch>>(req.downloadHandler.text);
            if (res.IsError) return new World[0];
            return res.data.worlds;
        }


        [ShareObjectExport] public Func<string, string, uint, uint, UniTask<ShareObject>> SharedSearchWorlds;
        [ShareObjectExport] public Func<string, uint[], UniTask<ShareObject[]>> SharedGetWorlds;

        public void BeforeExport()
        {
            SharedGetWorlds = async (server, worldIds) => await GetIWorlds(server, worldIds);
            SharedSearchWorlds = async (server, query, offset, limit) => await SearchIWorlds(server, query, offset, limit);
        }

        public void AfterExport()
        {
            SharedSearchWorlds = null;
            SharedGetWorlds = null;
        }
    }
}