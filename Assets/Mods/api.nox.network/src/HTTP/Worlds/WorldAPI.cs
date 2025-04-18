using System;
using System.Collections.Generic;
using System.IO;
using api.nox.network.HTTP;
using api.nox.network.Utils;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.network.Worlds
{
    public class WorldAPI : IDisposable, INoxObject
    {
        internal WorldAPI() => Asset = new Assets.AssetAPI();
        public void Dispose() => Asset = null;

        [NoxPublic(NoxAccess.Read)] public Assets.AssetAPI Asset;

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<World> CreateWorld(Dictionary<string, object> data)
            => await CreateWorld(CreateWorldData.From(data));

        private async UniTask<World> CreateWorld(CreateWorldData world)
        {
            if (NetworkSystem.ModInstance == null) throw new Exception("Network system is not initialized");
            // PUT /api/worlds

            var gateway = await Discover.GetGateway(world.Server);
            if (gateway == null) return null;

            var token = await NetworkSystem.ModInstance.Auth.GetToken(world.Server);
            if (token == null) return null;

            var request = new Request(Method.PUT, Request.MergeUrl(gateway, "/api/worlds"));

            var response =
                await request.Send<string, Response<World>>(world.ToJson(),
                    new() { { "Authorization", token.ToHeader() } });
            if (request.IsError || response.IsError) return null;

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("world_fetch", response.data));
            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("world_create", response.data));
            NetCache.Set(response.data);

            return response.data;
        }

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<bool> DeleteWorld(string server, uint worldId)
        {
            if (NetworkSystem.ModInstance == null) throw new Exception("Network system is not initialized");
            // DELETE /api/worlds/{worldId}

            var gateway = await Discover.GetGateway(server);
            if (gateway == null) return false;

            var token = await NetworkSystem.ModInstance.Auth.GetToken(server);
            if (token == null) return false;

            var request = new Request(Method.DELETE, Request.MergeUrl(gateway, $"/api/worlds/{worldId}"));
            var response = await request.Send<string, Response<object>>(null,
                new Dictionary<string, string> { { "Authorization", token.ToHeader() } });
            if (request.IsError || response.IsError) return false;
            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("world_delete", server, worldId));
            return true;
        }

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<bool> UploadWorldThumbnail(string server, uint worldId, string path)
        {
            if (NetworkSystem.ModInstance == null) throw new Exception("Network system is not initialized");
            // POST /api/worlds/{worldId}/thumbnail

            var gateway = await Discover.GetGateway(server);
            if (gateway == null) return false;

            var token = await NetworkSystem.ModInstance.Auth.GetToken(server);
            if (token == null) return false;

            var fileBytes = await IO.ReadFileAsync(path);
            var form = new WWWForm();
            form.AddBinaryData("file", fileBytes, Path.GetFileName(path));

            var request = new Request(Method.POST, Request.MergeUrl(gateway, $"/api/worlds/{worldId}/thumbnail"));

            var response = await request.Send<byte[], Response<object>>(form.data, new()
            {
                { "Authorization", token.ToHeader() },
                { "X-File-Hash", Hashing.HashFile(path) }
            });
            return !request.IsError && !response.IsError;
        }

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<World> GetWorld(string server, uint worldId)
        {
            if (NetworkSystem.ModInstance == null) throw new Exception("Network system is not initialized");
            // GET /api/worlds/{worldId}

            var gateway = await Discover.GetGateway(server);
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

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<World> UpdateWorld(Dictionary<string, object> data)
            => await UpdateWorld(UpdateWorldData.From(data));

        private async UniTask<World> UpdateWorld(UpdateWorldData world)
        {
            if (NetworkSystem.ModInstance == null) throw new Exception("Network system is not initialized");
            // POST /api/worlds/{worldId}

            var gateway = await Discover.GetGateway(world.Server);
            if (gateway == null) return null;

            var token = await NetworkSystem.ModInstance.Auth.GetToken(world.Server);
            if (token == null) return null;

            var request = new Request(Method.POST, Request.MergeUrl(gateway, $"/api/worlds/{world.WorldId}"));

            Logger.LogDebug($"Updating world {world.WorldId} on {world.Server}: {world.ToJson()}");
            var response = await request.Send<string, Response<World>>(
                world.ToJson(),
                new()
                {
                    { "Authorization", token.ToHeader() },
                    { "Content-Type", "application/json" }
                });
            if (request.IsError || response.IsError) return null;

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("world_fetch", response.data));
            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("world_update", response.data));
            NetCache.Set(response.data);

            return response.data;
        }

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<WorldSearch> SearchWorlds(Dictionary<string, object> data)
            => await SearchWorlds(SearchWorldData.From(data));

        internal async UniTask<WorldSearch> SearchWorlds(SearchWorldData data)
        {
            if (NetworkSystem.ModInstance == null) throw new Exception("Network system is not initialized");
            // GET /api/worlds/search?{data.ToParams()}

            var gateway = await Discover.GetGateway(data.Server);
            if (gateway == null) return null;

            var request = new Request(Method.GET, Request.MergeUrl(gateway, $"/api/worlds/search?{data.ToParams()}"));

            var token = await NetworkSystem.ModInstance.Auth.GetToken(data.Server);
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