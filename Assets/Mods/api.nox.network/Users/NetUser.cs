using System;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Mods;
using UnityEngine;
using UnityEngine.Networking;

namespace api.nox.network
{
    public class NetUser : ShareObject
    {
        private readonly NetworkSystem _mod;

        internal UserMe user;

        internal NetUser(NetworkSystem mod) => _mod = mod;

        public async UniTask<ShareObject> GetMyUser() => await GetMyIUser();
        private async UniTask<UserMe> GetMyIUser()
        {
            var config = Config.Load();
            if (!config.Has("token") || !config.Has("gateway"))
            {
                user = null;
                _mod._api.EventAPI.Emit(new NetEventContext("network.user", user, true));
                return null;
            }
            var req = new UnityWebRequest(string.Format("{0}/api/users/@me", config.Get<string>("gateway")), "GET") { downloadHandler = new DownloadHandlerBuffer() };
            req.SetRequestHeader("Authorization", string.Format("Bearer {0}", config.Get<string>("token")));
            try { await req.SendWebRequest(); }
            catch { }
            if (req.responseCode != 200)
            {
                user = null;
                _mod._api.EventAPI.Emit(new NetEventContext("network.user", user, true));
                return null;
            }
            var response = JsonUtility.FromJson<Response<UserMe>>(req.downloadHandler.text);
            user = response.data;
            _mod._api.EventAPI.Emit(new NetEventContext("network.user", user, true));
            return response.data;
        }

        public async UniTask<Response<bool>> GetLogout()
        {
            var config = Config.Load();
            if (!config.Has("token") || !config.Has("gateway")) return new Response<bool> { error = new ResponseError { code = 401, message = "Not logged in." } };
            var req = new UnityWebRequest(string.Format("{0}/api/auth/logout", config.Get<string>("gateway")), "GET") { downloadHandler = new DownloadHandlerBuffer() };
            req.SetRequestHeader("Authorization", string.Format("Bearer {0}", config.Get<string>("token")));
            try { await req.SendWebRequest(); }
            catch { }
            try
            {
                var response = JsonUtility.FromJson<Response<bool>>(req.downloadHandler.text);
                if (response.error?.code == 0)
                {
                    config.Remove("token");
                    config.Remove("user");
                    config.Remove("gateway");
                    config.Save();
                }
                user = null;
                _mod._api.EventAPI.Emit(new NetEventContext("network.user", user, true));
                _mod._api.EventAPI.Emit(new NetEventContext("network.user.logout", true, user));
                return new Response<bool> { data = true };
            }
            catch
            {
                _mod._api.EventAPI.Emit(new NetEventContext("network.user", user, true));
                _mod._api.EventAPI.Emit(new NetEventContext("network.user.logout", false, user));
                return new Response<bool> { error = new ResponseError { code = 500, message = "An error occured." } };
            };
        }

        public async UniTask<ShareObject> GetLogin(string server, string username, string password) => await PostILogin(server, username, password);
        private async UniTask<UserLogin> PostILogin(string server, string username, string password)
        {
            Uri gateway = await Gateway.FindGatewayMaster(server);
            if (gateway == null)
                return new UserLogin { error = "Server not found." };
            var req = new UnityWebRequest(string.Format("{0}/api/auth/login", gateway.OriginalString), "POST") { downloadHandler = new DownloadHandlerBuffer() };
            req.SetRequestHeader("Content-Type", "application/json");
            req.uploadHandler = new UploadHandlerRaw(
                System.Text.Encoding.UTF8.GetBytes(string.Format("{{\"identifier\":\"{0}\",\"password\":\"{1}\"}}", username, Hashing.Sha256(password)))
            );
            try { await req.SendWebRequest(); }
            catch { }
            try
            {
                var response = JsonUtility.FromJson<Response<UserLogin>>(req.downloadHandler.text);
                if (response.error?.code == 0)
                {
                    var config = Config.Load();
                    config.Set("token", response.data.token);
                    config.Set("user", response.data.user);
                    config.Set("gateway", gateway.OriginalString);
                    config.Save();
                }
                else response.data.error = response.error.message;
                user = response.data.user;
                _mod._api.EventAPI.Emit(new NetEventContext("network.user", user, true));
                _mod._api.EventAPI.Emit(new NetEventContext("network.user.login", true, user));
                return response.data;
            }
            catch
            {
                _mod._api.EventAPI.Emit(new NetEventContext("network.user", user, true));
                _mod._api.EventAPI.Emit(new NetEventContext("network.user.login", false, user));
                return new UserLogin { error = "An error occured." };
            }
        }


        public async UniTask<ShareObject> SearchUsers(string server, string query, uint offset = 0, uint limit = 10) => await SearchIUsers(server, query, offset, limit);
        private async UniTask<UserSearch> SearchIUsers(string server, string query, uint offset = 0, uint limit = 10)
        {
            // GET /api/users/search?query={query}&offset={offset}&limit={limit}
            var User = _mod.GetCurrentUser();
            var config = Config.Load();
            var gateway = server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null) return null;
            var req = new UnityWebRequest($"{gateway}/api/users/search?query={query}&offset={offset}&limit={limit}", "GET") { downloadHandler = new DownloadHandlerBuffer() };
            req.SetRequestHeader("Authorization", _mod.MostAuth(server));
            try { await req.SendWebRequest(); }
            catch { return null; }
            if (req.responseCode != 200) return null;
            var res = JsonUtility.FromJson<Response<UserSearch>>(req.downloadHandler.text);
            if (res.IsError) return null;
            return res.data;
        }
    }
}