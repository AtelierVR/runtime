using System;
using System.Collections.Generic;
using api.nox.network.HTTP;
using api.nox.network.Utils;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;

namespace api.nox.network.Servers
{
    public class ServerAPI : INoxObject
    {
        internal Server CurrentServer;

        [NoxPublic(NoxAccess.Method)]
        public Server GetCurrentServer() => CurrentServer;

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<Server> GetMyServer()
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");
            // GET /api/server
            var address = NetworkSystem.ModInstance.Auth.GetCurrentServerAddress();
            if (address == null) return null;

            var data = await GetServer(address);
            if (data == null) return null;

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("server_update", data));
            CurrentServer = data;

            var config = Config.Load();
            config.Set(new []{ "servers", data.address, "title" }, data.title);
            config.Set(new []{ "servers", data.address, "features" }, data.features);
            if (!config.Has(new []{ "servers", data.address, "navigation" }))
                config.Set(new []{ "servers", data.address, "navigation" }, false);
            config.Save();

            return data;
        }

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<Server> GetServer(string address)
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");
            // GET /api/server
            var gateway = await Gateway.FindGatewayMaster(address);
            if (gateway == null) return null;

            var request = new Request(Method.GET, Request.MergeUrl(gateway, "/api/server"));

            var token = await NetworkSystem.ModInstance.Auth.GetToken(address);
            var header = new Dictionary<string, string>();
            if (token != null) header.Add("Authorization", token.ToHeader());

            var response = await request.Send<string, Response<Server>>(null, header);
            if (request.IsError || response.IsError) return null;

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("server_fetch", response.data));
            NetCache.Set(response.data);

            var config = Config.Load();
            config.Set(new []{ "servers", response.data.address, "title" }, response.data.title);
            config.Set(new []{ "servers", response.data.address, "features" }, response.data.features);
            if (!config.Has(new []{ "servers", response.data.address, "navigation" }))
                config.Set(new []{ "servers", response.data.address, "navigation" }, false);
            config.Save();

            return response.data;
        }

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<SearchResponse> SearchServers(Dictionary<string, object> data)
            => await SearchServers(SearchRequest.From(data));

        internal async UniTask<SearchResponse> SearchServers(SearchRequest data)
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");
            // GET /api/servers/search?{data.ToParams()}
            var gateway = await Discover.GetGateway(data.Server);
            if (gateway == null) return null;

            var request = new Request(Method.GET, Request.MergeUrl(gateway, $"/api/servers/search?{data.ToParams()}"));

            var token = await NetworkSystem.ModInstance.Auth.GetToken(data.Server);
            var header = new Dictionary<string, string>();
            if (token != null) header.Add("Authorization", token.ToHeader());

            var response = await request.Send<string, Response<SearchResponse>>(null, header);
            if (request.IsError || response.IsError) return null;

            var config = Config.Load();
            foreach (var server in response.data.servers)
            {
                NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("server_fetch", server));
                NetCache.Set(server);

                config.Set(new []{ "servers", server.address, "title" }, server.title);
                config.Set(new []{ "servers", server.address, "features" }, server.features);
                if (!config.Has(new []{ "servers", server.address, "navigation" }))
                    config.Set(new []{ "servers", server.address, "navigation" }, false);
            }

            config.Save();

            return response.data;
        }
    }
}