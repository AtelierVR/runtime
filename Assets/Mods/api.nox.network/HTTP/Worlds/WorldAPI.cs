using System;
using System.Collections.Generic;
using System.IO;
using api.nox.network.HTTP;
using api.nox.network.Utils;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using UnityEngine;
using UnityEngine.Networking;

namespace api.nox.network.Worlds
{
    public class WorldAPI : IDisposable
    {
        public Assets.AssetAPI Asset;

        internal WorldAPI() => Asset = new Assets.AssetAPI();
        public void Dispose() => Asset = null;

        public async UniTask<World> CreateWorld(CreateWorldData world)
        {
            if (NetworkSystem.ModInstance == null) throw new Exception("Network system is not initialized");
            // PUT /api/worlds
            var gateway = await Gateway.FindGatewayMaster(world.server);
            if (gateway == null) return null;

            var token = await NetworkSystem.ModInstance.Auth.GetToken(world.server);
            if (token == null) return null;

            var request = new Request(Method.PUT, Request.MergeUrl(gateway, "/api/worlds"));
            
            var response = await request.Send<string, Response<World>>(world.ToJSON(), new() { { "Authorization", token.ToHeader() } });
            if (request.IsError || response.IsError) return null;

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("world_fetch", response.data));
            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("world_create", response.data));
            NetCache.Set(response.data);

            return response.data;
        }
        
        public async UniTask<bool> DeleteWorld(string server, uint worldId)
        {
            if (NetworkSystem.ModInstance == null) throw new Exception("Network system is not initialized");
            // DELETE /api/worlds/{worldId}
            var gateway = await Gateway.FindGatewayMaster(server);
            if (gateway == null) return false;

            var token = await NetworkSystem.ModInstance.Auth.GetToken(server);
            if (token == null) return false;

            var request = new Request(Method.DELETE, Request.MergeUrl(gateway, $"/api/worlds/{worldId}"));

            var response = await request.Send<string, Response<object>>(null, new() { { "Authorization", token.ToHeader() } });
            if (request.IsError || response.IsError) return false;

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("world_delete", server, worldId));

            return true;
        }

        public async UniTask<bool> UploadWorldThumbnail(string server, uint worldId, string path)
        {
            if (NetworkSystem.ModInstance == null) throw new Exception("Network system is not initialized");
            // POST /api/worlds/{worldId}/thumbnail
            var gateway = await Gateway.FindGatewayMaster(server);
            if (gateway == null) return false;

            var token = await NetworkSystem.ModInstance.Auth.GetToken(server);
            if (token == null) return false;

            byte[] fileBytes = await ReadFileAsync(path);
            var form = new WWWForm();
            form.AddBinaryData("file", fileBytes, Path.GetFileName(path));

            var request = new Request(Method.POST, Request.MergeUrl(gateway, $"/api/worlds/{worldId}/thumbnail"));

            var response = await request.Send<byte[], Response<object>>(form.data, new() { 
                { "Authorization", token.ToHeader() }, 
                { "X-File-Hash", Hashing.HashFile(path) }
            });
            if (request.IsError || response.IsError) return false;

            return true;
        }

        public async UniTask<World> GetWorld(string server, uint worldId)
        {
            if (NetworkSystem.ModInstance == null) throw new Exception("Network system is not initialized");
            // GET /api/worlds/{worldId}
            var gateway = await Gateway.FindGatewayMaster(server);
            if (gateway == null) return null;

            var request = new Request(Method.GET, Request.MergeUrl(gateway, $"/api/worlds/{worldId}"));

            var token = await NetworkSystem.ModInstance.Auth.GetToken(server);
            var header = new Dictionary<string, string> { };
            if (token != null) header.Add("Authorization", token.ToHeader());

            var response = await request.Send<string, Response<World>>(null, header);
            if (request.IsError || response.IsError) return null;

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("world_fetch", response.data));
            NetCache.Set(response.data);

            return response.data;
        }

        public async UniTask<World> UpdateWorld(UpdateWorldData world)
        {
            if (NetworkSystem.ModInstance == null) throw new Exception("Network system is not initialized");
            // POST /api/worlds/{worldId}
            var gateway = await Gateway.FindGatewayMaster(world.server);
            if (gateway == null) return null;

            var token = await NetworkSystem.ModInstance.Auth.GetToken(world.server);
            if (token == null) return null;

            var request = new Request(Method.POST, Request.MergeUrl(gateway, $"/api/worlds/{world.worldId}"));

            var response = await request.Send<string, Response<World>>(world.ToJSON(), new() { { "Authorization", token.ToHeader() } });
            if (request.IsError || response.IsError) return null;

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("world_fetch", response.data));
            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("world_update", response.data));
            NetCache.Set(response.data);

            return response.data;
        }

        internal static async UniTask<byte[]> ReadFileAsync(string path)
        {
            using FileStream sourceStream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            byte[] buffer = new byte[sourceStream.Length];
            await sourceStream.ReadAsync(buffer, 0, (int)sourceStream.Length);
            return buffer;
        }

        public async UniTask<WorldSearch> SearchWorlds(SearchWorldData data)
        {
            if (NetworkSystem.ModInstance == null) throw new Exception("Network system is not initialized");
            // GET /api/worlds/search?{data.ToParams()}
            var gateway = await Gateway.FindGatewayMaster(data.server);
            if (gateway == null) return null;

            var request = new Request(Method.GET, Request.MergeUrl(gateway, $"/api/worlds/search?{data.ToParams()}"));

            var token = await NetworkSystem.ModInstance.Auth.GetToken(data.server);
            var header = new Dictionary<string, string> { };
            if (token != null) header.Add("Authorization", token.ToHeader());

            var response = await request.Send<string, Response<WorldSearch>>(null, header);
            if (request.IsError || response.IsError) return null;

            foreach (var world in response.data.worlds)
            {
                NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("world_fetch", world));
                NetCache.Set(world);
            }

            return response.data;
        }
    }
}