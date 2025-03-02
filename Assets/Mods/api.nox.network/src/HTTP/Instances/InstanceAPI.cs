using System;
using System.Collections.Generic;
using api.nox.network.HTTP;
using api.nox.network.Utils;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace api.nox.network.Instances
{
    public class InstanceAPI : INoxObject
    {
        [NoxPublic(NoxAccess.Method)]
        public async UniTask<Instance> GetInstance(string server, uint instanceId)
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");
            // GET /api/instances/{instanceId}
            var gateway = await Discover.GetGateway(server);
            if (gateway == null) return null;

            var request = new Request(Method.GET, Request.MergeUrl(gateway, $"/api/instances/{instanceId}"));

            var token = await NetworkSystem.ModInstance.Auth.GetToken(server);
            var header = new Dictionary<string, string> { };
            if (token != null) header.Add("Authorization", token.ToHeader());

            var response = await request.Send<string, Response<Instance>>(null, header);
            if (request.IsError || response.IsError) return null;

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("instance_fetch", response.data));
            NetCache.Set(response.data);

            return response.data;
        }

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<SearchResponse> SearchInstances(Dictionary<string, object> data)
            => await SearchInstances(SearchRequest.From(data));

        internal static async UniTask<SearchResponse> SearchInstances(SearchRequest data)
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");
            // GET /api/instances/search?{data.ToParams()}
            var gateway = await Discover.GetGateway(data.Server);
            if (gateway == null) return null;

            var request = new Request(Method.GET,
                Request.MergeUrl(gateway, $"/api/instances/search?{data.ToParams()}"));

            var token = await NetworkSystem.ModInstance.Auth.GetToken(data.Server);
            var header = new Dictionary<string, string> { };
            if (token != null) header.Add("Authorization", token.ToHeader());

            var response = await request.Send<string, Response<SearchResponse>>(null, header);
            if (request.IsError || response.IsError) return null;

            foreach (var instance in response.data.instances)
            {
                NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("instance_fetch", instance));
                NetCache.Set(instance);
            }

            return response.data;
        }

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<Instance> CreateInstance(Dictionary<string, object> data)
            => await CreateInstance(CreateRequest.From(data));

        private async UniTask<Instance> CreateInstance(CreateRequest data)
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");
            // PUT /api/instances
            var gateway = await Discover.GetGateway(data.Server);
            if (gateway == null) return null;

            var token = await NetworkSystem.ModInstance.Auth.GetToken(data.Server);
            if (token == null) return null;

            var request = new Request(Method.PUT, Request.MergeUrl(gateway, "/api/instances"));

            var response = await request.Send<string, Response<Instance>>(data.ToJson(), new()
            {
                { "Authorization", token.ToHeader() },
                { "Content-Type", "application/json" }
            });
            if (request.IsError || response.IsError) return null;

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("instance_fetch", response.data));
            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("instance_created", response.data));
            NetCache.Set(response.data);

            return response.data;
        }
    }
}