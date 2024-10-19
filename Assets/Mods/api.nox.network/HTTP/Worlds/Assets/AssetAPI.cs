using System.Collections.Generic;
using System.IO;
using api.nox.network.HTTP;
using api.nox.network.Utils;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using UnityEngine;

namespace api.nox.network.Worlds.Assets
{
    public class AssetAPI
    {
        public async UniTask<WorldAsset> GetAsset(string server, uint worldId, uint assetId)
        {
            if (NetworkSystem.ModInstance == null) throw new System.Exception("NetSystem not initialized");
            // GET /api/worlds/{worldId}/assets/{assetId}
            var gateway = await Gateway.FindGatewayMaster(server);
            if (gateway == null) return null;

            var request = new Request(Method.GET, Request.MergeUrl(gateway, $"/api/worlds/{worldId}/assets/{assetId}"));

            var token = await NetworkSystem.ModInstance.Auth.GetToken(server);
            var header = new Dictionary<string, string> { };
            if (token != null) header.Add("Authorization", token.ToHeader());

            var response = await request.Send<string, Response<WorldAsset>>(null, header);
            if (request.IsError || response.IsError) return null;

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("world_asset_fetch", response.data));
            NetCache.Set(response.data);

            return response.data;
        }

        // private async UniTask<WorldAsset> UpdateAsset(UpdateAssetData asset)
        // {
        //     var User = _mod._api.NetworkAPI.CallMethod<User>("GetCurrentUser");
        //     if (User == null) return null;
        //     Debug.Log("Updating asset");
        //     var config = Config.Load();
        //     var gateway = asset.server == User.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(asset.server))?.OriginalString;
        //     if (gateway == null) return null;
        //     var req = new UnityWebRequest(Request.MergeUrl(gateway, "/api/worlds/{asset.worldId}/assets/{asset.id}", "POST")) { downloadHandler = new DownloadHandlerBuffer() };
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
            if (NetworkSystem.ModInstance == null) throw new System.Exception("NetSystem not initialized");
            // DELETE /api/worlds/{worldId}/assets/{assetId}
            var gateway = await Gateway.FindGatewayMaster(server);
            if (gateway == null) return false;

            var token = await NetworkSystem.ModInstance.Auth.GetToken(server);
            if (token == null) return false;

            var request = new Request(Method.DELETE, Request.MergeUrl(gateway, $"/api/worlds/{worldId}/assets/{assetId}"));

            var response = await request.Send<string, Response<bool>>(null, new() { { "Authorization", token.ToHeader() } });
            if (request.IsError || response.IsError) return false;

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("world_asset_delete", server, worldId, assetId));

            return true;
        }

        public async UniTask<bool> UploadAssetFile(string server, uint worldId, uint assetId, string path)
        {
            if (NetworkSystem.ModInstance == null) throw new System.Exception("NetSystem not initialized");
            // POST /api/worlds/{worldId}/assets/{assetId}/file
            var gateway = await Gateway.FindGatewayMaster(server);
            if (gateway == null) return false;

            var token = await NetworkSystem.ModInstance.Auth.GetToken(server);
            if (token == null) return false;

            byte[] fileBytes = await WorldAPI.ReadFileAsync(path);
            var form = new WWWForm();
            form.AddBinaryData("file", fileBytes, Path.GetFileName(path));

            var request = new Request(Method.POST, Request.MergeUrl(gateway, $"/api/worlds/{worldId}/assets/{assetId}/file"));

            var response = await request.Send<WWWForm, Response<bool>>(form, new() {
                { "Authorization", token.ToHeader() },
                { "X-File-Hash", Hashing.HashFile(path) }
            });
            if (request.IsError || response.IsError) return false;

            return true;
        }

        public async UniTask<WorldAsset> CreateAsset(CreateAssetData asset)
        {
            if (NetworkSystem.ModInstance == null) throw new System.Exception("NetSystem not initialized");
            // PUT /api/worlds/{worldId}/assets
            var gateway = await Gateway.FindGatewayMaster(asset.server);
            if (gateway == null) return null;

            var token = await NetworkSystem.ModInstance.Auth.GetToken(asset.server);
            if (token == null) return null;

            var request = new Request(Method.PUT, Request.MergeUrl(gateway, $"/api/worlds/{asset.worldId}/assets"));

            var response = await request.Send<CreateAssetData, Response<WorldAsset>>(asset, new() { { "Authorization", token.ToHeader() } });
            if (request.IsError || response.IsError) return null;

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("world_asset_fetch", response.data));
            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("world_asset_create", response.data));
            NetCache.Set(response.data);

            return response.data;
        }

        public async UniTask<SearchResponse> SearchAssets(SearchRequest data)
        {
            if (NetworkSystem.ModInstance == null) throw new System.Exception("NetSystem not initialized");
            // GET /api/worlds/{id}/assets/search?{data.ToParams()}
            var gateway = await Gateway.FindGatewayMaster(data.server);
            if (gateway == null) return null;

            var request = new Request(Method.GET, Request.MergeUrl(gateway, $"/api/worlds/{data.world_id}/assets?{data.ToParams()}"));

            var token = await NetworkSystem.ModInstance.Auth.GetToken(data.server);
            var header = new Dictionary<string, string> { };
            if (token != null) header.Add("Authorization", token.ToHeader());

            var response = await request.Send<string, Response<SearchResponse>>(null, header);
            if (request.IsError || response.IsError) return null;

            response.data.with_empty = data.with_empty;

            foreach (var asset in response.data.assets)
            {
                asset.server = data.server;
                asset.world_id = data.world_id;
                NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("world_asset_fetch", response.data));
            }

            return response.data;
        }
    }
}