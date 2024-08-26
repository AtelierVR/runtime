using System;
using api.nox.network.Utils;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Mods;
using UnityEngine;
using UnityEngine.Networking;

namespace api.nox.network
{
    public class UserAPI : ShareObject
    {
        private readonly NetworkSystem _mod;

        internal UserMe user;

        internal UserAPI(NetworkSystem mod) => _mod = mod;

        private async UniTask<UserMe> GetMyUser()
        {
            var config = Config.Load();
            if (!config.Has("token") || !config.Has("gateway")) return null;
            var req = new UnityWebRequest(string.Format("{0}/api/users/@me", config.Get<string>("gateway")), "GET") { downloadHandler = new DownloadHandlerBuffer() };
            req.SetRequestHeader("Authorization", string.Format("Bearer {0}", config.Get<string>("token")));
            try { await req.SendWebRequest(); }
            catch { }
            if (req.responseCode != 200) return null;
            var response = JsonUtility.FromJson<Response<UserMe>>(req.downloadHandler.text);
            if (response.IsError) return null;
            response.data.netSystem = _mod;
            user = response.data;
            _mod._api.EventAPI.Emit(new NetEventContext("network.get.user.me", user, true));
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
            _mod._api.EventAPI.Emit(new NetEventContext("network.logout.user.me", user, true));
            config.Remove("token");
            config.Remove("user");
            config.Remove("gateway");
            config.Save();
            user = null;
            return new Response<bool> { data = true };
        }

        public async UniTask<UserIntegrity> CreateIntegrity(string address)
        {
            var config = Config.Load();
            if (!config.Has("token") || !config.Has("gateway")) return null;
            var req = new UnityWebRequest(string.Format("{0}/api/users/@me/integrity", config.Get<string>("gateway")), "GET") { downloadHandler = new DownloadHandlerBuffer() };
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", string.Format("Bearer {0}", config.Get<string>("token")));
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new UserIntegrityRequest
            {
                address = address
            })));
            try { await req.SendWebRequest(); }
            catch { }
            if (req.responseCode != 200) return null;
            var response = JsonUtility.FromJson<Response<UserIntegrity>>(req.downloadHandler.text);
            if (response.IsError) return response.data;
            config.Set($"servers.{address}.integrity.token", response.data.token);
            config.Set($"servers.{address}.integrity.expires", response.data.expires);
            config.Save();
            return response.data;
        }

        public async UniTask<UserLogin> PostLogin(string server, string username, string password)
        {
            Uri gateway = await Gateway.FindGatewayMaster(server);
            if (gateway == null) return new UserLogin { error = "Server not found." };
            var req = new UnityWebRequest(string.Format("{0}/api/auth/login", gateway.OriginalString), "POST") { downloadHandler = new DownloadHandlerBuffer() };
            req.SetRequestHeader("Content-Type", "application/json");
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new UserLoginRequest
            {
                identifier = username,
                password = Hashing.Sha256(password)
            })));
            try { await req.SendWebRequest(); }
            catch { }
            if (req.responseCode != 200) return new UserLogin { error = "An error occured." };
            var response = JsonUtility.FromJson<Response<UserLogin>>(req.downloadHandler.text);
            if (response.IsError) return response.data;
            var config = Config.Load();
            config.Set("token", response.data.token);
            config.Set("user", response.data.user);
            config.Set("gateway", gateway.OriginalString);
            config.Save();
            response.data.user.netSystem = _mod;
            user = response.data.user;
            _mod._api.EventAPI.Emit(new NetEventContext("network.login.user.me", user.server, user.id, user));
            _mod._api.EventAPI.Emit(new NetEventContext("network.get.user.me", user.server, user.id, user));
            return response.data;
        }

        public async UniTask<UserSearch> SearchUsers(string server, string query, uint offset = 0, uint limit = 10)
        {
            // GET /api/users/search?query={query}&offset={offset}&limit={limit}
            var User = _mod.GetCurrentUser();
            var config = Config.Load();
            var gateway = server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null) return null;
            var req = new UnityWebRequest($"{gateway}/api/users/search?query={query}&offset={offset}&limit={limit}", "GET") { downloadHandler = new DownloadHandlerBuffer() };
            var token = await _mod._auth.GetToken(server);
            if (token != null) req.SetRequestHeader("Authorization", token.ToHeader());
            try { await req.SendWebRequest(); }
            catch { }
            if (req.responseCode != 200) return null;
            var res = JsonUtility.FromJson<Response<UserSearch>>(req.downloadHandler.text);
            if (res.IsError) return null;
            _mod._api.EventAPI.Emit(new NetEventContext("network.search.user", server, query, offset, limit, res.data));
            foreach (var user in res.data.users)
                _mod._api.EventAPI.Emit(new NetEventContext("network.get.user", user.server, user.id, user));
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
            _mod._api.EventAPI.Emit(new NetEventContext("network.get.user.me", res.data.server, res.data.id, res.data));
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