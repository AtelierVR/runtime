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

        private async UniTask<UserMe> GetMyUser()
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
            if(response.IsError) return null;
            response.data.netSystem = _mod;
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
                var response = JsonUtility.FromJson<Response<UserLogout>>(req.downloadHandler.text);
                if (response.error?.code == 0 || response.data.success)
                {
                    config.Remove("token");
                    config.Remove("user");
                    config.Remove("gateway");
                    config.Save();

                    user = null;
                    _mod._api.EventAPI.Emit(new NetEventContext("network.user", user, true));
                    _mod._api.EventAPI.Emit(new NetEventContext("network.user.logout", true, user));
                    return new Response<bool> { data = true };
                }
                else throw new Exception();
            }
            catch
            {
                _mod._api.EventAPI.Emit(new NetEventContext("network.user", user, true));
                _mod._api.EventAPI.Emit(new NetEventContext("network.user.logout", false, user));
                return new Response<bool> { error = new ResponseError { code = 500, message = "An error occured." } };
            };
        }

        public async UniTask<UserLogin> PostLogin(string server, string username, string password)
        {
            Uri gateway = await Gateway.FindGatewayMaster(server);
            if (gateway == null)
                return new UserLogin { error = "Server not found." };
            var req = new UnityWebRequest(string.Format("{0}/api/auth/login", gateway.OriginalString), "POST") { downloadHandler = new DownloadHandlerBuffer() };
            req.SetRequestHeader("Content-Type", "application/json");
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new UserLoginRequest
            {
                identifier = username,
                password = Hashing.Sha256(password)
            })));
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
                response.data.user.netSystem = _mod;
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

        public async UniTask<UserSearch> SearchUsers(string server, string query, uint offset = 0, uint limit = 10)
        {
            // GET /api/users/search?query={query}&offset={offset}&limit={limit}
            var User = _mod.GetCurrentUser();
            var config = Config.Load();
            var gateway = server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null) return null;
            var req = new UnityWebRequest($"{gateway}/api/users/search?query={query}&offset={offset}&limit={limit}", "GET") { downloadHandler = new DownloadHandlerBuffer() };
            if (_mod.TryMostAuth(server, out var auth)) req.SetRequestHeader("Authorization", auth);
            try { await req.SendWebRequest(); }
            catch { return null; }
            if (req.responseCode != 200) return null;
            var res = JsonUtility.FromJson<Response<UserSearch>>(req.downloadHandler.text);
            if (res.IsError) return null;
            return res.data;
        }

        public async UniTask<UserMe> UpdateUser(UserUpdate user)
        {
            var config = Config.Load();
            if (!config.Has("token") || !config.Has("gateway")) return null;
            var req = new UnityWebRequest($"{config.Get<string>("gateway")}/api/users/@me", "POST") { downloadHandler = new DownloadHandlerBuffer() };
            req.SetRequestHeader("Authorization", string.Format("Bearer {0}", config.Get<string>("token")));
            req.SetRequestHeader("Content-Type", "application/json");
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(user)));
            try { await req.SendWebRequest(); }
            catch { return null; }
            if (req.responseCode != 200) return null;
            var res = JsonUtility.FromJson<Response<UserMe>>(req.downloadHandler.text);
            if (res.IsError) return null;
            res.data.netSystem = _mod;
            this.user = res.data;
            return res.data;
        }

        [ShareObjectExport] public Func<string, string, uint, uint, UniTask<ShareObject>> SharedSearchUsers;
        [ShareObjectExport] public Func<UniTask<ShareObject>> SharedGetMyUser;
        [ShareObjectExport] public Func<ShareObject, UniTask<ShareObject>> SharedUpdateUser;
        [ShareObjectExport] public Func<UniTask<bool>> SharedGetLogout;
        [ShareObjectExport] public Func<string, string, string, UniTask<ShareObject>> SharedPostLogin;

        public void BeforeExport()
        {
            SharedSearchUsers = async (server, query, offset, limit) => await SearchUsers(server, query, offset, limit);
            SharedUpdateUser = async (user) => await UpdateUser(user.Convert<UserUpdate>());
            SharedGetMyUser = async () => await GetMyUser();
            SharedGetLogout = async () => (await GetLogout()).data == true;
            SharedPostLogin = async (server, username, password) => await PostLogin(server, username, password);
        }

        public void AfterExport()
        {
            SharedGetMyUser = null;
            SharedSearchUsers = null;
            SharedUpdateUser = null;
        }
    }
}