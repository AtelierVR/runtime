using System;
using System.IO;
using api.nox.network.Utils;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Mods;
using UnityEngine;
using UnityEngine.Networking;

namespace api.nox.network
{
    public class WorldAPI : ShareObject
    {
        private readonly NetworkSystem _mod;
        internal WorldAPI(NetworkSystem mod) => _mod = mod;
        internal async UniTask<WorldAsset> GetAsset(string server, uint worldId, uint assetId)
        {
            var User = _mod.GetCurrentUser();
            if (User == null) return null;
            var config = Config.Load();
            var gateway = server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null) return null;
            var req = new UnityWebRequest($"{gateway}/api/worlds/{worldId}/assets/{assetId}", "GET") { downloadHandler = new DownloadHandlerBuffer() };
            var token = await _mod._auth.GetToken(server);
            if (token != null) req.SetRequestHeader("Authorization", token.ToHeader());
            try { await req.SendWebRequest(); }
            catch { return null; }
            if (req.responseCode != 200) return null;
            Debug.Log(req.downloadHandler.text);
            var res = JsonUtility.FromJson<Response<WorldAsset>>(req.downloadHandler.text);
            if (res.IsError) return null;
            res.data.server = server;
            _mod._api.EventAPI.Emit(new NetEventContext("network.get.world.asset", server, worldId, assetId, res.data));
            return res.data;
        }

        // public async UniTask<ShareObject> UpdateAsset(UpdateAssetData asset) => await UpdateIAsset(asset);
        // private async UniTask<WorldAsset> UpdateAsset(UpdateAssetData asset)
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

        internal async UniTask<bool> DeleteAsset(string server, uint worldId, uint assetId)
        {
            var User = _mod.GetCurrentUser();
            if (User == null) return false;
            var config = Config.Load();
            var gateway = server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null) return false;
            var req = new UnityWebRequest($"{gateway}/api/worlds/{worldId}/assets/{assetId}", "DELETE") { downloadHandler = new DownloadHandlerBuffer() };
            var token = await _mod._auth.GetToken(server);
            if (token != null) req.SetRequestHeader("Authorization", token.ToHeader());
            try { await req.SendWebRequest(); }
            catch { return false; }
            if (req.responseCode != 200) return false;
            _mod._api.EventAPI.Emit(new NetEventContext("network.delete.world.asset", server, worldId, assetId));
            return true;
        }

        internal async UniTask<bool> DeleteWorld(string server, uint worldId)
        {
            var User = _mod.GetCurrentUser();
            if (User == null) return false;
            var config = Config.Load();
            var gateway = server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null) return false;
            var req = new UnityWebRequest($"{gateway}/api/worlds/{worldId}", "DELETE") { downloadHandler = new DownloadHandlerBuffer() };
            var token = await _mod._auth.GetToken(server);
            if (token != null) req.SetRequestHeader("Authorization", token.ToHeader());
            try { await req.SendWebRequest(); }
            catch { return false; }
            if (req.responseCode != 200) return false;
            _mod._api.EventAPI.Emit(new NetEventContext("network.delete.world", server, worldId));
            return true;
        }

        internal async UniTask<bool> UploadAssetFile(string server, uint worldId, uint assetId, string path)
        {
            var User = _mod.GetCurrentUser();
            if (User == null) return false;
            var config = Config.Load();
            var gateway = server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null) return false;
            byte[] fileBytes = await ReadFileAsync(path);
            var form = new WWWForm();
            form.AddBinaryData("file", fileBytes, Path.GetFileName(path));
            var req = UnityWebRequest.Post($"{gateway}/api/worlds/{worldId}/assets/{assetId}/file", form);
            var token = await _mod._auth.GetToken(server);
            if (token != null) req.SetRequestHeader("Authorization", token.ToHeader());
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

        internal async UniTask<bool> UploadWorldThumbnail(string server, uint worldId, string path)
        {
            var User = _mod.GetCurrentUser();
            if (User == null) return false;
            var config = Config.Load();
            var gateway = server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null) return false;
            byte[] fileBytes = await ReadFileAsync(path);
            var form = new WWWForm();
            form.AddBinaryData("file", fileBytes, Path.GetFileName(path));
            var req = UnityWebRequest.Post($"{gateway}/api/worlds/{worldId}/thumbnail", form);
            var token = await _mod._auth.GetToken(server);
            if (token != null) req.SetRequestHeader("Authorization", token.ToHeader());
            req.SetRequestHeader("X-File-Hash", Hashing.HashFile(path));
            try { await req.SendWebRequest(); }
            catch { return false; }
            if (req.responseCode != 200) return false;
            return true;
        }

        internal async UniTask<WorldAsset> CreateAsset(CreateAssetData asset)
        {
            var User = _mod.GetCurrentUser();
            if (User == null) return null;
            Debug.Log("Creating asset");
            var config = Config.Load();
            var gateway = asset.server == User.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(asset.server))?.OriginalString;
            if (gateway == null) return null;
            var req = new UnityWebRequest($"{gateway}/api/worlds/{asset.worldId}/assets", "PUT") { downloadHandler = new DownloadHandlerBuffer() };
            var token = await _mod._auth.GetToken(asset.server);
            if (token != null) req.SetRequestHeader("Authorization", token.ToHeader());
            req.SetRequestHeader("Content-Type", "application/json");
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(asset.ToJSON()));
            try { await req.SendWebRequest(); }
            catch { return null; }
            Debug.Log(req.downloadHandler.text);
            if (req.responseCode != 200) return null;
            var res = JsonUtility.FromJson<Response<WorldAsset>>(req.downloadHandler.text);
            if (res.IsError) return null;
            res.data.server = asset.server;
            _mod._api.EventAPI.Emit(new NetEventContext("network.create.world.asset", asset.server, asset.worldId, res.data));
            return res.data;
        }

        internal async UniTask<World> CreateWorld(CreateWorldData world)
        {
            var User = _mod.GetCurrentUser();
            if (User == null) return null;
            Debug.Log("Creating world");
            var config = Config.Load();
            var gateway = world.server == User.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(world.server))?.OriginalString;
            if (gateway == null) return null;
            var req = new UnityWebRequest($"{gateway}/api/worlds", "PUT") { downloadHandler = new DownloadHandlerBuffer() };
            var token = await _mod._auth.GetToken(world.server);
            if (token != null) req.SetRequestHeader("Authorization", token.ToHeader());
            req.SetRequestHeader("Content-Type", "application/json");
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(world.ToJSON()));
            try { await req.SendWebRequest(); }
            catch { return null; }
            Debug.Log(req.downloadHandler.text);
            if (req.responseCode != 200) return null;
            Debug.Log(req.downloadHandler.text);
            var res = JsonUtility.FromJson<Response<World>>(req.downloadHandler.text);
            if (res.IsError) return null;
            res.data.networkSystem = _mod;
            _mod._api.EventAPI.Emit(new NetEventContext("network.create.world", world.server, res.data));
            return res.data;
        }

        internal async UniTask<World> GetWorld(string server, uint worldId)
        {
            var User = _mod.GetCurrentUser();
            if (User == null) return null;
            var config = Config.Load();
            var gateway = server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null) return null;
            var req = new UnityWebRequest($"{gateway}/api/worlds/{worldId}", "GET") { downloadHandler = new DownloadHandlerBuffer() };
            var token = await _mod._auth.GetToken(server);
            if (token != null) req.SetRequestHeader("Authorization", token.ToHeader());
            try { await req.SendWebRequest(); }
            catch { return null; }
            if (req.responseCode != 200) return null;
            var res = JsonUtility.FromJson<Response<World>>(req.downloadHandler.text);
            if (res.IsError) return null;
            res.data.networkSystem = _mod;
            _mod._api.EventAPI.Emit(new NetEventContext("network.get.world", server, worldId, res.data));
            return res.data;
        }

        private async UniTask<World> UpdateWorld(UpdateWorldData world)
        {
            var User = _mod.GetCurrentUser();
            var config = Config.Load();
            var gateway = world.server == User.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(world.server))?.OriginalString;
            if (gateway == null) return null;
            var req = new UnityWebRequest($"{gateway}/api/worlds/{world.worldId}", "POST") { downloadHandler = new DownloadHandlerBuffer() };
            var token = await _mod._auth.GetToken(world.server);
            if (token != null) req.SetRequestHeader("Authorization", token.ToHeader());
            req.SetRequestHeader("Content-Type", "application/json");
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(world.ToJSON()));
            try { await req.SendWebRequest(); }
            catch { return null; }
            if (req.responseCode != 200) return null;
            var res = JsonUtility.FromJson<Response<World>>(req.downloadHandler.text);
            if (res.IsError) return null;
            res.data.networkSystem = _mod;
            _mod._api.EventAPI.Emit(new NetEventContext("network.update.world", world.server, world.worldId, res.data));
            return res.data;
        }

        async UniTask<byte[]> ReadFileAsync(string path)
        {
            using FileStream sourceStream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            byte[] buffer = new byte[sourceStream.Length];
            await sourceStream.ReadAsync(buffer, 0, (int)sourceStream.Length);
            return buffer;
        }

        internal async UniTask<WorldSearch> SearchWorlds(string server, string query, uint offset = 0, uint limit = 10)
        {
            // GET /api/worlds/search?query={query}&offset={offset}&limit={limit}
            var User = _mod.GetCurrentUser();
            var config = Config.Load();
            var gateway = server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null) return null;
            var req = new UnityWebRequest($"{gateway}/api/worlds/search?query={query}&offset={offset}&limit={limit}", "GET") { downloadHandler = new DownloadHandlerBuffer() };
            var token = await _mod._auth.GetToken(server);
            if (token != null) req.SetRequestHeader("Authorization", token.ToHeader());
            try { await req.SendWebRequest(); }
            catch { return null; }
            if (req.responseCode != 200) return null;
            var res = JsonUtility.FromJson<Response<WorldSearch>>(req.downloadHandler.text);
            if (res.IsError) return null;
            Debug.Log("Searched world: " + req.downloadHandler.text);
            Debug.Log("Searched world: " + res.data);
            foreach (var world in res.data.worlds)
            {
                world.networkSystem = _mod;
                _mod._api.EventAPI.Emit(new NetEventContext("network.get.world", server, world.id, world));
            }
            _mod._api.EventAPI.Emit(new NetEventContext("network.search.world", server, query, offset, limit, res.data));
            return res.data;
        }

        internal async UniTask<World[]> GetWorlds(string server, uint[] worldIds)
        {
            var User = _mod.GetCurrentUser();
            if (User == null) return new World[0];
            var config = Config.Load();
            var gateway = server == User.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null) return new World[0];
            var req = new UnityWebRequest($"{gateway}/api/worlds/search?id={string.Join("&id=", worldIds)}", "GET") { downloadHandler = new DownloadHandlerBuffer() };
            var token = await _mod._auth.GetToken(server);
            if (token != null) req.SetRequestHeader("Authorization", token.ToHeader());
            try { await req.SendWebRequest(); }
            catch { return new World[0]; }
            if (req.responseCode != 200) return new World[0];
            var res = JsonUtility.FromJson<Response<WorldSearch>>(req.downloadHandler.text);
            if (res.IsError) return new World[0];
            foreach (var world in res.data.worlds)
            {
                world.networkSystem = _mod;
                _mod._api.EventAPI.Emit(new NetEventContext("network.get.world", server, world.id, world));
            }
            return res.data.worlds;
        }

        internal async UniTask<WorldAssetSearch> SearchAssets(string server, uint id, uint offset, uint limit, uint[] versions = null, string[] platforms = null, string[] engines = null, bool withEmpty = false)
        {
            // GET /api/worlds/{id}/assets/search?offset={offset}&limit={limit}&version={version[0]}&version={version[1]}&platform={platform[0]}&platform={platform[1]}&engine={engine[0]}&engine={engine[1]}&empty={withEmpty}  
            var User = _mod.GetCurrentUser();
            var config = Config.Load();
            var gateway = server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null)
            {
                Debug.Log("Gateway is null");
                return null;
            }
            string url = $"{gateway}/api/worlds/{id}/assets?offset={offset}&limit={limit}";
            if (versions != null) foreach (var version in versions) url += $"&version={version}";
            if (platforms != null) foreach (var platform in platforms) url += $"&platform={platform}";
            if (engines != null) foreach (var engine in engines) url += $"&engine={engine}";
            if (withEmpty) url += "&empty=true";
            var req = new UnityWebRequest(url, "GET") { downloadHandler = new DownloadHandlerBuffer() };
            var token = await _mod._auth.GetToken(server);
            if (token != null) req.SetRequestHeader("Authorization", token.ToHeader());
            try { await req.SendWebRequest(); }
            catch
            {
                Debug.Log("Error: " + req.downloadHandler.text);
                return null;
            }
            if (req.responseCode != 200)
            {
                Debug.Log("Error: " + req.downloadHandler.text);
                return null;
            }
            var res = JsonUtility.FromJson<Response<WorldAssetSearch>>(req.downloadHandler.text);
            if (res.IsError)
            {
                Debug.Log("Error: " + req.downloadHandler.text);
                return null;
            }
            res.data.netSystem = _mod;
            res.data.server = server;
            res.data.world_id = id;
            res.data.versions = versions;
            res.data.platforms = platforms;
            res.data.withEmpty = withEmpty;
            res.data.engines = engines;
            foreach (var asset in res.data.assets)
            {
                asset.server = server;
                _mod._api.EventAPI.Emit(new NetEventContext("network.get.world.asset", server, id, asset.id, asset));
            }
            _mod._api.EventAPI.Emit(new NetEventContext("network.search.world.asset", server, id, offset, limit, versions, platforms, engines, withEmpty, res.data));
            return res.data;
        }


        [ShareObjectExport] public Func<string, string, uint, uint, UniTask<ShareObject>> SharedSearchWorlds;
        [ShareObjectExport] public Func<string, uint[], UniTask<ShareObject[]>> SharedGetWorlds;
        [ShareObjectExport] public Func<string, uint, uint, uint, uint[], string[], string[], bool, UniTask<ShareObject>> SharedSearchAssets;
        [ShareObjectExport] public Func<string, uint, uint, UniTask<ShareObject>> SharedGetAsset;
        [ShareObjectExport] public Func<string, uint, UniTask<ShareObject>> SharedGetWorld;
        [ShareObjectExport] public Func<ShareObject, UniTask<ShareObject>> SharedCreateWorld;
        [ShareObjectExport] public Func<ShareObject, UniTask<ShareObject>> SharedCreateAsset;
        [ShareObjectExport] public Func<string, uint, uint, string, UniTask<bool>> SharedUploadAssetFile;
        [ShareObjectExport] public Func<ShareObject, UniTask<ShareObject>> SharedUpdateWorld;

        public void BeforeExport()
        {
            SharedGetWorlds = async (server, worldIds) => await GetWorlds(server, worldIds);
            SharedSearchWorlds = async (server, query, offset, limit) => await SearchWorlds(server, query, offset, limit);
            SharedSearchAssets = async (server, worldId, offset, limit, versions, platforms, engines, withEmpty) => await SearchAssets(server, worldId, offset, limit, versions, platforms, engines, withEmpty);
            SharedGetAsset = async (server, worldId, assetId) => await GetAsset(server, worldId, assetId);
            SharedGetWorld = async (server, worldId) => await GetWorld(server, worldId);
            SharedCreateWorld = async (world) => await CreateWorld(world?.Convert<CreateWorldData>());
            SharedCreateAsset = async (asset) => await CreateAsset(asset?.Convert<CreateAssetData>());
            SharedUploadAssetFile = async (server, worldId, assetId, path) => await UploadAssetFile(server, worldId, assetId, path);
            SharedUpdateWorld = async (world) => await UpdateWorld(world?.Convert<UpdateWorldData>());
        }

        public void AfterExport()
        {
            SharedSearchWorlds = null;
            SharedGetWorlds = null;
            SharedSearchAssets = null;
            SharedGetAsset = null;
            SharedGetWorld = null;
            SharedCreateWorld = null;
            SharedCreateAsset = null;
            SharedUploadAssetFile = null;
            SharedUpdateWorld = null;
        }

    }
}