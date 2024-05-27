using System;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Mods;
using Nox.CCK.Users;
using UnityEngine;
using UnityEngine.Networking;

namespace api.nox.network
{
    public class NetUser
    {
        private readonly NetworkSystem _mod;

        internal User user;

        internal NetUser(NetworkSystem mod) => _mod = mod;
        
        public async UniTask<User> FetchUserMe()
        {
            var config = Config.Load();
            if (!config.Has("token") || !config.Has("gateway")) return null;
            var req = new UnityWebRequest(string.Format("{0}/api/users/@me", config.Get<string>("gateway")), "GET") { downloadHandler = new DownloadHandlerBuffer() };
            req.SetRequestHeader("Authorization", string.Format("Bearer {0}", config.Get<string>("token")));
            try { await req.SendWebRequest(); }
            catch { }
            if (req.responseCode != 200) return null;
            var response = JsonUtility.FromJson<Response<User>>(req.downloadHandler.text);
            return response.data;
        }

        public async UniTask<Response<bool>> FetchLogout()
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
                return new Response<bool> { data = true };
            }
            catch
            {
                return new Response<bool> { error = new ResponseError { code = 500, message = "An error occured." } };
            };
        }

        public async UniTask<Response<Login>> FetchLogin(string server, string username, string password)
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
                return response;
            }
            catch
            {
                return new Response<Login> { error = new ResponseError { code = 500, message = "An error occured." } };
            }
        }
    }

    [Serializable]
    public class Login : ShareObject
    {
        public string token;
        public User user;
    }
}