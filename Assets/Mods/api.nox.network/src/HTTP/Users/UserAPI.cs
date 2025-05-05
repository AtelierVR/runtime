using System;
using System.Collections.Generic;
using api.nox.network.HTTP;
using api.nox.network.Utils;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace api.nox.network.Users
{
    public class UserAPI : INoxObject
    {
        public UserMe CurrentUser { get; internal set; }

        [NoxPublic(NoxAccess.Method)]
        public UserMe GetCurrentUser() => CurrentUser;

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<UserResponse> SearchUsers(Dictionary<string, object> data)
            => await SearchUsers(SearchRequest.From(data));

        internal async UniTask<UserResponse> SearchUsers(SearchRequest data)
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");
            // GET /api/users/search?{data.ToParams()}
            var gateway = await Discover.GetGateway(data.Server);
            if (gateway == null) return null;

            var request = new Request(Method.GET, Request.MergeUrl(gateway, $"/api/users/search?{data.ToParams()}"));

            var token = await NetworkSystem.ModInstance.Auth.GetToken(data.Server);
            var header = new Dictionary<string, string>();
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

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<UserMe> GetMyUser()
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");
            // GET /api/users/@me
            var address = NetworkSystem.ModInstance.Auth.GetCurrentServerAddress();
            if (address == null)
            {
                Logger.LogDebug("No server address found");
                return null;
            }

            var gateway = await Gateway.FindGatewayMaster(address);
            if (gateway == null)
            {
                Logger.LogDebug("No gateway found");
                return null;
            }

            var request = new Request(Method.GET, Request.MergeUrl(gateway, "/api/users/@me"));

            var token = await NetworkSystem.ModInstance.Auth.GetToken(address);
            if (token == null)
            {
                Logger.LogDebug("No token found");
                return null;
            }

            var response =
                await request.Send<string, Response<UserMe>>(null, new() { { "Authorization", token.ToHeader() } });
            if (request.IsError || response.IsError)
            {
                Logger.LogDebug("Request error");
                return null;
            }

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("user_fetch", response.data));
            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("user_update", response.data));
            NetCache.Set(response.data);
            CurrentUser = response.data;

            Logger.LogDebug("User fetched");
            return response.data;
        }

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<User> GetUserById(string server, uint id)
            => await GetUserByIdentifier(id.ToString(), server);

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<User> GetUserByIdentifier(string identifier, string defaultServer)
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");
            // GET /api/users/{identifier}
            var ide = UserIdentifier.FromString(identifier);
            if (ide.IsLocal()) ide.Server = defaultServer;
            
            var gateway = await Discover.GetGateway(ide.Server);
            if (gateway == null) return null;

            var request = new Request(Method.GET, Request.MergeUrl(gateway, $"/api/users/{identifier}"));

            var token = await NetworkSystem.ModInstance.Auth.GetToken(ide.Server);
            var header = new Dictionary<string, string>();
            if (token != null) header.Add("Authorization", token.ToHeader());

            var response = await request.Send<string, Response<User>>(null, header);
            if (request.IsError || response.IsError) return null;

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("user_fetch", response.data));
            NetCache.Set(response.data);

            return response.data;
        }

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<UserMe> UpdateMyUser(Dictionary<string, object> data)
            => await UpdateMyUser(UserUpdate.From(data));

        public async UniTask<UserMe> UpdateMyUser(UserUpdate user)
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");
            // POST /api/users/@me
            var server = NetworkSystem.ModInstance.Auth.GetCurrentServerAddress();
            if (server == null) return null;

            var gateway = await Discover.GetGateway(server);
            if (gateway == null) return null;

            var token = await NetworkSystem.ModInstance.Auth.GetToken(server);
            if (token == null) return null;

            var request = new Request(Method.POST, Request.MergeUrl(gateway, "/api/users/@me"));

            var response = await request.Send<string, Response<UserMe>>(user.ToJson(), new()
            {
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

        [NoxPublic(NoxAccess.Method)]
        public UserIdentifier MakeIdentifierFromString(string identifier)
            => UserIdentifier.FromString(identifier);

        [NoxPublic(NoxAccess.Method)]
        public UserIdentifier MakeIdentifier(string id, string server)
            => new(id, server);
    }
}