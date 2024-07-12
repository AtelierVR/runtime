using System;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Mods.Networks;
using Nox.CCK.Users;
using UnityEngine;
using UnityEngine.Networking;

namespace api.nox.network
{
    public class NetUser : NetworkAPIUser
    {
        private readonly NetworkSystem _mod;

        internal UserMe user;

        internal NetUser(NetworkSystem mod) => _mod = mod;

        public async UniTask<UserMe> GetMyUser()
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

        public async UniTask<Response<Login>> PostLogin(string server, string username, string password)
        {
            Uri gateway = await Gateway.FindGatewayMaster(server);
            if (gateway == null)
                return new Response<Login> { error = new ResponseError { code = 404, message = "Server not found." } };
            var req = new UnityWebRequest(string.Format("{0}/api/auth/login", gateway.OriginalString), "POST") { downloadHandler = new DownloadHandlerBuffer() };
            req.SetRequestHeader("Content-Type", "application/json");
            req.uploadHandler = new UploadHandlerRaw(
                System.Text.Encoding.UTF8.GetBytes(string.Format("{{\"identifier\":\"{0}\",\"password\":\"{1}\"}}", username, Hashing.Sha256(password)))
            );
            try { await req.SendWebRequest(); }
            catch { }
            try
            {
                var response = JsonUtility.FromJson<Response<Login>>(req.downloadHandler.text);
                Debug.Log(req.downloadHandler.text);
                if (response.error?.code == 0)
                {
                    var config = Config.Load();
                    config.Set("token", response.data.token);
                    config.Set("user", response.data.user);
                    config.Set("gateway", gateway.OriginalString);
                    config.Save();
                }
                user = response.data.user;
                _mod._api.EventAPI.Emit(new NetEventContext("network.user", user, true));
                _mod._api.EventAPI.Emit(new NetEventContext("network.user.login", true, user));
                return response;
            }
            catch
            {
                _mod._api.EventAPI.Emit(new NetEventContext("network.user", user, true));
                _mod._api.EventAPI.Emit(new NetEventContext("network.user.login", false, user));
                return new Response<Login> { error = new ResponseError { code = 500, message = "An error occured." } };
            }
        }
    }
}