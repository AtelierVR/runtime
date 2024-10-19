using System;
using System.Collections.Generic;
using api.nox.network.HTTP;
using api.nox.network.Utils;
using Cysharp.Threading.Tasks;

namespace api.nox.network.Users
{
    public class UserAPI
    {
        public UserMe CurrentUser { get; internal set; }

        public async UniTask<UserMe> GetMyUser()
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");
            // GET /api/users/@me
            var address = NetworkSystem.ModInstance.Auth.CurrentServerAddress;
            if (address == null) return null;

            var gateway = await Gateway.FindGatewayMaster(address);
            if (gateway == null) return null;

            var request = new Request(Method.GET, Request.MergeUrl(gateway, "/api/users/@me"));

            var token = await NetworkSystem.ModInstance.Auth.GetToken(address);
            if (token == null) return null;

            var response = await request.Send<string, Response<UserMe>>(null, new() { { "Authorization", token.ToHeader() } });
            if (request.IsError || response.IsError) return null;

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("user_fetch", response.data));
            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("user_update", response.data));
            NetCache.Set(response.data);
            CurrentUser = response.data;

            return response.data;
        }

        public async UniTask<User> GetUser(string server, uint id) => await GetUser(server, id.ToString());
        public async UniTask<User> GetUser(string server, string identifier)
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");
            // GET /api/users/{identifier}
            var gateway = await Discover.GetGateway(server);
            if (gateway == null) return null;

            var request = new Request(Method.GET, Request.MergeUrl(gateway, $"/api/users/{identifier}"));

            var token = await NetworkSystem.ModInstance.Auth.GetToken(server);
            var header = new Dictionary<string, string> { };
            if (token != null) header.Add("Authorization", token.ToHeader());

            var response = await request.Send<string, Response<User>>(null, header);
            if (request.IsError || response.IsError) return null;

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("user_fetch", response.data));
            NetCache.Set(response.data);

            return response.data;
        }



        public async UniTask<UserResponse> SearchUsers(SearchRequest data)
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");
            // GET /api/users/search?{data.ToParams()}
            var gateway = await Discover.GetGateway(data.server);
            if (gateway == null) return null;

            var request = new Request(Method.GET, Request.MergeUrl(gateway, $"/api/users/search?{data.ToParams()}"));

            var token = await NetworkSystem.ModInstance.Auth.GetToken(data.server);
            var header = new Dictionary<string, string> { };
            if (token != null) header.Add("Authorization", token.ToHeader());

            var response = await request.Send<string, Response<UserResponse>>(null, header);
            if (request.IsError || response.IsError) return null;

            foreach (var user in response.data.users)
            {
                NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("user_fetch", user));
                NetCache.Set(user);
            }

            return response.data;
        }

        public async UniTask<UserMe> UpdateMyUser(UserUpdate user)
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");
            // POST /api/users/@me
            var server = NetworkSystem.ModInstance.Auth.CurrentServerAddress;
            if (server == null) return null;

            var gateway = await Discover.GetGateway(server);
            if (gateway == null) return null;

            var token = await NetworkSystem.ModInstance.Auth.GetToken(server);
            if (token == null) return null;

            var request = new Request(Method.POST, Request.MergeUrl(gateway, "/api/users/@me"));

            var response = await request.Send<string, Response<UserMe>>(user.ToJson(), new() {
                { "Authorization", token.ToHeader() },
                { "Content-Type", "application/json" }
            });
            if (request.IsError || response.IsError) return null;

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("user_update", response.data));
            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("user_fetch", response.data));
            NetCache.Set(response.data);
            CurrentUser = response.data;

            return response.data;
        }
    }
}