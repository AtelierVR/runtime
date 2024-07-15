// using Cysharp.Threading.Tasks;
// using Nox.CCK;
// using Nox.Scripts;
// using UnityEngine;
// using UnityEngine.Networking;

// namespace Nox.Users
// {
//     public class UserManager : Manager<User>
//     {
//         public static UserMe CurrentUser() => Cache.Find(i => i.IsMe) as UserMe;
//         public static User Get(uint id, string serverAddress) => Cache.Find(i => i.IsMe);
//         public static async UniTask<User> GetOrFetch(uint id, string serverAddress) => Get(id, serverAddress) ?? await Fetch(id, serverAddress);

//         public static async UniTask<User> Fetch(uint id, string serverAddress)
//         {
//             var gateway = await Gateway.FindGatewayMaster(serverAddress);
//             if (gateway == null) return null;
//             var req = new UnityWebRequest($"{gateway}/api/users/{id}", "GET")
//             { downloadHandler = new DownloadHandlerBuffer() };
//             req.SetRequestHeader("Authorization", Lookup.MostAuth(serverAddress));
//             try { await req.SendWebRequest(); }
//             catch { return null; }
//             if (req.responseCode != 200) return null;
//             var res = JsonUtility.FromJson<Response<User>>(req.downloadHandler.text);
//             if (res.IsError) return null;
//             res.data.IsMe = false;
//             return Set(res.data);
//         }

//         public static async UniTask<UserMe> FetchMe()
//         {
//             var config = Config.Load();
//             if (!config.Has("token") || !config.Has("gateway")) return null;
//             var req = new UnityWebRequest($"{config.Get<string>("gateway")}/api/users/@me", "GET")
//             { downloadHandler = new DownloadHandlerBuffer() };
//             req.SetRequestHeader("Authorization", $"Bearer {config.Get<string>("token")}");
//             try { await req.SendWebRequest(); }
//             catch { return null; }
//             if (req.responseCode != 200) return null;
//             var res = JsonUtility.FromJson<Response<UserMe>>(req.downloadHandler.text);
//             if (res.IsError) return null;
//             res.data.IsMe = true;
//             Set(res.data);
            
//             return res.data;
//         }
//     }
// }