using System;
using api.nox.network.HTTP;
using api.nox.network.Users;
using api.nox.network.Utils;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using UnityEngine;

namespace api.nox.network.Auths
{
    public class AuthAPI
    {
        public string CurrentServerAddress
        {
            get => Config.Load().Get<string>("server", null);
            internal set => Config.Load().Set("server", value);
        }

        public async UniTask<AuthToken> GetToken(string address)
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");

            var config = Config.Load();

            var server = CurrentServerAddress;
            if (string.IsNullOrEmpty(server)) return null;

            var hashserver = Animator.StringToHash(address);
            if (server == address)
            {
                if (config.Has($"servers.{hashserver}.token"))
                {
                    var expires = config.Get($"servers.{hashserver}.expires", ulong.MinValue);
                    if (expires != ulong.MinValue && expires > (ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds())
                        return new AuthToken() { token = config.Get<string>($"servers.{hashserver}.token") };
                }
                return null;
            }

            // If the server has an integrity token, return it
            var userhash = Animator.StringToHash(server);
            if (config.Has($"servers.{userhash}.integrity.{hashserver}.token"))
            {
                var expires = config.Get($"servers.{userhash}.integrity.{hashserver}.expires", ulong.MinValue);
                if (expires != ulong.MinValue && expires > (ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds())
                    return new AuthToken() { token = config.Get<string>($"servers.{userhash}.integrity.{hashserver}.token"), isIntegrity = true };
            }

            // Create a new integrity token
            var result = await CreateIntegrity(address);
            if (result != null && !result.IsExpirated())
            {
                config.Set($"servers.{userhash}.integrity.{hashserver}.token", result.token);
                config.Set($"servers.{userhash}.integrity.{hashserver}.expires", result.expires);
                config.Save();
                return new AuthToken() { token = result.token, isIntegrity = true };
            }

            return null;
        }

        public async UniTask<bool> Logout()
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");
            // GET /api/auth/logout
            var address = CurrentServerAddress;
            if (address == null) return false;

            var gateway = await Gateway.FindGatewayMaster(address);
            if (gateway == null) return false;

            var token = await GetToken(address);
            if (token == null) return false;

            var request = new Request(Method.GET, Request.MergeUrl(gateway, "/api/auth/logout"));

            var response = await request.Send<string, Response<bool>>(null, new() { { "Authorization", token.ToHeader() } });
            if (request.IsError || response.IsError)
                Debug.LogError(response.error.message);

            var config = Config.Load();
            var hash = Animator.StringToHash(address);

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("user_disconnect", NetworkSystem.ModInstance.User.CurrentUser));

            CurrentServerAddress = null;

            // remove server token
            config.Remove($"servers.{hash}.token");
            config.Remove($"servers.{hash}.expires");

            // remove integrity token
            config.Remove($"servers.{hash}.integrity");

            config.Save();

            NetworkSystem.ModInstance.User.CurrentUser = null;
            NetworkSystem.ModInstance.Server.CurrentServer = null;

            return true;
        }

        public async UniTask<LoginResponse> Login(string address, string username, string password)
            => await Login(new LoginRequest { server = address, identifier = username, password = Hashing.Sha256(password) });
            
        public async UniTask<LoginResponse> Login(LoginRequest login)
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");
            // POST /api/auth/login
            var gateway = await Discover.GetGateway(login.server);
            if (gateway == null) return new LoginResponse { error = "Server not found." };

            var request = new Request(Method.POST, Request.MergeUrl(gateway, "/api/auth/login"));

            var response = await request.Send<string, Response<LoginResponse>>(login.ToJSON(), new() { { "Content-Type", "application/json" } });
            if (request.IsError || response.IsError) return response.data;

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("user_connect", response.data.user));
            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("user_update", response.data.user));

            var serverhash = Animator.StringToHash(response.data.user.server);

            var config = Config.Load();

            config.Set($"servers.{serverhash}.token", response.data.token);
            config.Set($"servers.{serverhash}.expires", response.data.expires);
            config.Remove($"servers.{serverhash}.integrity");
            CurrentServerAddress = response.data.user.server;

            config.Save();


            NetworkSystem.ModInstance.User.CurrentUser = response.data.user;
            NetworkSystem.ModInstance.Server.CurrentServer = await NetworkSystem.ModInstance.Server.GetServer(response.data.user.server);

            return response.data;
        }


        public async UniTask<Integrity> CreateIntegrity(string address)
        {
            if (NetworkSystem.ModInstance == null) throw new AccessViolationException("NetworkSystem not initialized");
            // PUT /api/users/@me/integrity
            var server = CurrentServerAddress;
            if (string.IsNullOrEmpty(server)) return null;

            var gateway = await Discover.GetGateway(server);
            if (gateway == null) return null;

            var token = await GetToken(server);
            if (token == null) return null;

            var request = new Request(Method.PUT, Request.MergeUrl(gateway, "/api/users/@me/integrity"));

            var response = await request.Send<IntegrityRequest, Response<Integrity>>(new IntegrityRequest { address = address }, new() { { "Authorization", token.ToHeader() } });
            if (request.IsError || response.IsError) return null;

            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("integrity_create", response.data));

            var config = Config.Load();
            var userhash = Animator.StringToHash(server);
            var serverhash = Animator.StringToHash(address);

            config.Set($"servers.{userhash}.integrity.{serverhash}.token", response.data.token);
            config.Set($"servers.{userhash}.integrity.{serverhash}.expires", response.data.expires);

            config.Save();

            return response.data;
        }
    }
}