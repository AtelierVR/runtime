
using System;
using System.Collections.Generic;
using api.nox.network.HTTP;
using api.nox.network.Utils;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using UnityEngine;

namespace api.nox.network.Servers
{
    public class ServerAPI
    {
        public Server CurrentServer { get; internal set; }

        public async UniTask<Server> GetMyServer()
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");
            // GET /api/server
            var address = NetworkSystem.ModInstance.Auth.CurrentServerAddress;
            if (address == null) return null;

            var data = await GetServer(address);
            if (data == null) return null;

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("server_update", data));
            CurrentServer = data;

            var serverhash = Animator.StringToHash(data.address);
            var config = Config.Load();
            config.Set($"servers.{serverhash}.title", data.title);
            config.Set($"servers.{serverhash}.address", data.address);
            config.Set($"servers.{serverhash}.features", data.features);
            if (!config.Has($"servers.{serverhash}.navigation"))
                config.Set($"servers.{serverhash}.navigation", false);
            config.Save();

            return data;
        }

        public async UniTask<Server> GetServer(string address)
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");
            // GET /api/server
            var gateway = await Gateway.FindGatewayMaster(address);
            if (gateway == null) return null;

            var request = new Request(Method.GET, Request.MergeUrl(gateway, "/api/server"));

            var token = await NetworkSystem.ModInstance.Auth.GetToken(address);
            var header = new Dictionary<string, string> { };
            if (token != null) header.Add("Authorization", token.ToHeader());

            var response = await request.Send<string, Response<Server>>(null, header);
            if (request.IsError || response.IsError) return null;

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("server_fetch", response.data));
            NetCache.Set(response.data);

            var serverhash = Animator.StringToHash(response.data.address);
            var config = Config.Load();
            config.Set($"servers.{serverhash}.title", response.data.title);
            config.Set($"servers.{serverhash}.address", response.data.address);
            config.Set($"servers.{serverhash}.features", response.data.features);
            if (!config.Has($"servers.{serverhash}.navigation"))
                config.Set($"servers.{serverhash}.navigation", false);
            config.Save();

            return response.data;
        }

        public async UniTask<SearchResponse> SearchServers(SearchRequest data)
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");
            // GET /api/servers/search?{data.ToParams()}
            var gateway = await Discover.GetGateway(data.server);
            if (gateway == null) return null;

            var request = new Request(Method.GET, Request.MergeUrl(gateway, $"/api/servers/search?{data.ToParams()}"));

            var token = await NetworkSystem.ModInstance.Auth.GetToken(data.server);
            var header = new Dictionary<string, string> { };
            if (token != null) header.Add("Authorization", token.ToHeader());

            var response = await request.Send<string, Response<SearchResponse>>(null, header);
            if (request.IsError || response.IsError) return null;

            var config = Config.Load();
            foreach (var server in response.data.servers)
            {
                NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("server_fetch", server));
                NetCache.Set(server);

                var serverhash = Animator.StringToHash(server.address);
                config.Set($"servers.{serverhash}.title", server.title);
                config.Set($"servers.{serverhash}.address", server.address);
                config.Set($"servers.{serverhash}.features", server.features);
                if (!config.Has($"servers.{serverhash}.navigation"))
                    config.Set($"servers.{serverhash}.navigation", false);
            }
            config.Save();

            return response.data;
        }
    }
}