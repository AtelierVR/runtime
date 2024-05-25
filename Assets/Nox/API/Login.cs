// using System;
// using Cysharp.Threading.Tasks;
// using Nox.CCK;
// using Nox.Servers;
// using Nox.Users;
// using UnityEngine;
// using UnityEngine.Networking;

// namespace Nox.API
// {
//     public class Login
//     {
//         public static async UniTask<LoginResponse> LoginUser(LoginRequest body)
//         {
//             var gateway = (await ServerManager.GetOrFetch(body.server))?.gateways;
//             if (gateway == null) return new LoginResponse() { result = LoginResult.ServerError };
//             var req = new UnityWebRequest($"{gateway}/api/auth/login", "POST")
//             { downloadHandler = new DownloadHandlerBuffer() };
//             req.SetRequestHeader("Content-Type", "application/json");
//             body.password = Hashing.Sha256(body.password);
//             req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));
//             try { await req.SendWebRequest(); } catch { }
//             Response<LoginResponse> res = null;
//             try { res = JsonUtility.FromJson<Response<LoginResponse>>(req.downloadHandler.text); }
//             catch { return new LoginResponse() { result = LoginResult.ServerError }; }
//             if (res.IsError)
//                 return new LoginResponse() { result = LoginResult.InvalidCredentials };
//             var config = Config.Load();
//             config.Set("token", res.data.token);
//             config.Set("gateway", gateway.http);
//             config.Save();
//             res.data.user.IsMe = true;
//             var olduser = UserManager.CurrentUser();
//             if (olduser != null) olduser.IsMe = false;
//             UserManager.Set(res.data.user);
//             return res.data;
//         }
//     }

//     [Serializable]
//     public class LoginRequest
//     {
//         [NonSerialized] public string server;
//         public string identifier;
//         public string password;
//     }

//     [Serializable]
//     public class LoginResponse
//     {
//         public LoginResult result;
//         public string token;
//         public ulong expires;
//         public UserMe user;
//     }

//     public enum LoginResult
//     {
//         Success,
//         InvalidCredentials,
//         PasswordRequired,
//         ServerError
//     }
// }