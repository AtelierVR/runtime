using System;
using System.Collections.Generic;
using api.nox.network.HTTP;
using api.nox.network.Utils;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.network.Auths
{
    public class AuthAPI : INoxObject
    {
        [NoxPublic(NoxAccess.Method)]
        public string GetCurrentServerAddress()
            => Config.Load().Get<string>("server");

        private void SetCurrentServerAddress(string address)
        {
            var config = Config.Load();
            config.Set("server", address);
            config.Save();
        }

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<bool> Logout()
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");
            var address = GetCurrentServerAddress();
            if (address == null) return false;
            var gateway = await Gateway.FindGatewayMaster(address);
            if (gateway == null) return false;
            var token = await GetToken(address);
            if (token == null) return false;
            var request = new Request(Method.GET, Request.MergeUrl(gateway, "/api/auth/logout"));
            var response = await request.Send<string, Response<bool>>(null,
                new Dictionary<string, string> { { "Authorization", token.ToHeader() } });
            if (request.IsError || response.IsError)
                Logger.LogError(response.error.message);
            var config = Config.Load();

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext(
                "user_disconnect",
                NetworkSystem.ModInstance.User.CurrentUser
            ));
            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext(
                "server_disconnect",
                NetworkSystem.ModInstance.Server.CurrentServer
            ));
            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("user_update", null));
            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("server_update", null));

            NetworkSystem.ModInstance.User.CurrentUser = null;
            NetworkSystem.ModInstance.Server.CurrentServer = null;

            SetCurrentServerAddress(null);
            config.Remove(new[] { "servers", address, "token" });
            config.Remove(new[] { "servers", address, "expires" });
            config.Remove(new[] { "servers", address, "user_id" });
            config.Remove(new[] { "servers", address, "integrity" });
            config.Save();
            return true;
        }

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<LoginResponse> Login(string address, string username, string password)
            => await Login(new LoginRequest
                { server = address, identifier = username, password = Hashing.Sha256(password) });

        private async UniTask<LoginResponse> Login(LoginRequest login)
        {
            var gateway = await Discover.GetGateway(login.server);
            if (gateway == null) return new LoginResponse { error = "Server not found." };
            var request = new Request(Method.POST, Request.MergeUrl(gateway, "/api/auth/login"));
            var response = await request.Send<string, Response<LoginResponse>>(login.ToJSON(),
                new Dictionary<string, string> { { "Content-Type", "application/json" } });
            if (response == null)
                return new LoginResponse { error = "Response is null" };
            if (request.IsError || response.IsError) return response.data;
            var server = await NetworkSystem.ModInstance.Server.GetServer(response.data.user.server);
            if (server == null) return new LoginResponse { error = "Server not found." };

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("user_connect", response.data.user));
            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("server_connect", server));
            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("user_update", response.data.user));
            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("user_fetch", response.data.user));

            NetworkSystem.ModInstance.User.CurrentUser = response.data.user;
            NetworkSystem.ModInstance.Server.CurrentServer = server;

            var config = Config.Load();

            config.Set(new[] { "servers", response.data.user.server, "token" }, response.data.token);
            config.Set(new[] { "servers", response.data.user.server, "expires" }, response.data.expires);
            config.Set(new[] { "servers", response.data.user.server, "user_id" }, response.data.user.id);
            config.Remove(new[] { "servers", response.data.user.server, "integrity" });
            config.Save();
            SetCurrentServerAddress(response.data.user.server);


            return response.data;
        }

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<Integrity> CreateIntegrity(string address)
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");

            var server = GetCurrentServerAddress();
            if (string.IsNullOrEmpty(server)) return null;

            var gateway = await Discover.GetGateway(server);
            if (gateway == null) return null;

            var token = await GetToken(server);
            if (token == null) return null;

            var request = new Request(Method.PUT, Request.MergeUrl(gateway, "/api/users/@me/integrity"));

            var response = await request.Send<IntegrityRequest, Response<Integrity>>(
                new IntegrityRequest { address = address },
                new Dictionary<string, string> { { "Authorization", token.ToHeader() } });
            if (request.IsError || response.IsError) return null;

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("integrity_create", response.data));

            var config = Config.Load();
            config.Set(new[] { "servers", server, "integrity", address, "token" }, response.data.token);
            config.Set(new[] { "servers", server, "integrity", address, "expires" }, response.data.expires);
            config.Save();

            return response.data;
        }

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<AuthToken> GetToken(string address)
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");

            var config = Config.Load();

            var server = GetCurrentServerAddress();
            if (string.IsNullOrEmpty(server)) return null;

            if (server == address)
            {
                if (!config.Has(new[] { "servers", address, "token" })) return null;
                var expires = config.Get(new[] { "servers", address, "expires" }, long.MinValue);
                if (expires > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                    return new AuthToken { Token = config.Get<string>(new[] { "servers", address, "token" }) };
                return null;
            }

            // If the server has an integrity token, return it
            if (config.Has(new[] { "servers", server, "integrity", address, "token" }))
            {
                var expires = config.Get(new[] { "servers", server, "integrity", address, "expires" }, long.MinValue);
                if (expires > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                    return new AuthToken
                    {
                        Token = config.Get<string>(new[] { "servers", server, "integrity", address, "token" }),
                        IsIntegrity = true
                    };
            }

            // Create a new integrity token
            var result = await CreateIntegrity(address);
            if (result != null && !result.IsExpired())
            {
                config.Set(new[] { "servers", server, "integrity", address, "token" }, result.token);
                config.Set(new[] { "servers", server, "integrity", address, "expires" }, result.expires);
                config.Save();
                return new AuthToken
                {
                    Token = result.token,
                    IsIntegrity = true
                };
            }

            return null;
        }
    }
}