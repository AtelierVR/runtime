// #if UNITY_EDITOR
// using System.IO;
// using System.Linq;
// using System.Threading.Tasks;
// using Cysharp.Threading.Tasks;
// using Nox.CCK;
// using Nox.CCK.Editor;
// using Nox.CCK.Users;
// using Nox.CCK.Worlds;
// using UnityEngine;
// using UnityEngine.Networking;

// namespace api.nox.world
// {
//     public class Net
//     {
//         public static User User => CCKModManager.Get("api.nox.user").Property<User>("User");

//         public static string MostAuth(string server)
//         {
//             var config = Config.Load();
//             if (User.server == server && config.Has("token"))
//                 return $"Bearer {config.Get<string>("token")}";
//             return null;
//         }

//         public static async UniTask<World> GetWorld(string server, uint worldId, bool withEmpty = false)
//         {
//             if (User == null) return null;
//             Debug.Log("Getting world");
//             var config = Config.Load();
//             var gateway = server == User.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
//             if (gateway == null) return null;
//             var req = new UnityWebRequest($"{gateway}/api/worlds/{worldId}{(withEmpty ? "?empty" : "")}", "GET") { downloadHandler = new DownloadHandlerBuffer() };
//             req.SetRequestHeader("Authorization", MostAuth(server));
//             try { await req.SendWebRequest(); }
//             catch { return null; }
//             if (req.responseCode != 200) return null;
//             var res = JsonUtility.FromJson<Response<World>>(req.downloadHandler.text);
//             if (res.IsError) return null;
//             Debug.Log(req.downloadHandler.text);
//             return res.data;
//         }

//         public static async UniTask<World> UpdateWorld(UpdateWorldData world, bool withEmpty = false)
//         {
//             if (User == null) return null;
//             Debug.Log("Updating world");
//             var config = Config.Load();
//             var gateway = world.server == User.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(world.server))?.OriginalString;
//             if (gateway == null) return null;
//             var req = new UnityWebRequest($"{gateway}/api/worlds/{world.id}{(withEmpty ? "?empty" : "")}", "POST") { downloadHandler = new DownloadHandlerBuffer() };
//             req.SetRequestHeader("Authorization", MostAuth(world.server));
//             req.SetRequestHeader("Content-Type", "application/json");
//             req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(world)));
//             try { await req.SendWebRequest(); }
//             catch { return null; }
//             if (req.responseCode != 200) return null;
//             var res = JsonUtility.FromJson<Response<World>>(req.downloadHandler.text);
//             if (res.IsError) return null;
//             return res.data;
//         }

//         [System.Serializable]
//         public class UpdateWorldData
//         {
//             [System.NonSerialized] public string server;
//             [System.NonSerialized] public uint id;
//             public string title;
//             public string description;
//             public ushort capacity;
//             public string thumbnail;
//         }

//         public static async UniTask<World> CreateWorld(CreateWorldData world)
//         {
//             if (User == null) return null;
//             Debug.Log("Creating world");
//             var config = Config.Load();
//             var gateway = world.server == User.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(world.server))?.OriginalString;
//             if (gateway == null) return null;
//             var req = new UnityWebRequest($"{gateway}/api/worlds", "PUT") { downloadHandler = new DownloadHandlerBuffer() };
//             req.SetRequestHeader("Authorization", MostAuth(world.server));
//             req.SetRequestHeader("Content-Type", "application/json");
//             req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(world)));
//             try { await req.SendWebRequest(); }
//             catch { return null; }
//             Debug.Log(req.downloadHandler.text);
//             if (req.responseCode != 200) return null;
//             Debug.Log(req.downloadHandler.text);
//             var res = JsonUtility.FromJson<Response<World>>(req.downloadHandler.text);
//             if (res.IsError) return null;
//             return res.data;
//         }

//         [System.Serializable]
//         public class CreateWorldData
//         {
//             [System.NonSerialized] public string server;
//             public uint id;
//             public string title;
//             public string description;
//             public ushort capacity;
//             public string thumbnail;
//         }

//         public static async UniTask<Asset> GetAsset(string server, uint worldId, uint assetId)
//         {
//             if (User == null) return null;
//             Debug.Log("Getting asset");
//             var config = Config.Load();
//             var gateway = server == User.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
//             if (gateway == null) return null;
//             var req = new UnityWebRequest($"{gateway}/api/worlds/{worldId}/assets/{assetId}", "GET") { downloadHandler = new DownloadHandlerBuffer() };
//             req.SetRequestHeader("Authorization", MostAuth(server));
//             try { await req.SendWebRequest(); }
//             catch { return null; }
//             if (req.responseCode != 200) return null;
//             Debug.Log(req.downloadHandler.text);
//             var res = JsonUtility.FromJson<Response<Asset>>(req.downloadHandler.text);
//             if (res.IsError) return null;
//             return res.data;
//         }

//         public static async UniTask<Asset> UpdateAsset(UpdateAssetData asset)
//         {
//             if (User == null) return null;
//             Debug.Log("Updating asset");
//             var config = Config.Load();
//             var gateway = asset.server == User.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(asset.server))?.OriginalString;
//             if (gateway == null) return null;
//             var req = new UnityWebRequest($"{gateway}/api/worlds/{asset.worldId}/assets/{asset.id}", "POST") { downloadHandler = new DownloadHandlerBuffer() };
//             req.SetRequestHeader("Authorization", MostAuth(asset.server));
//             req.SetRequestHeader("Content-Type", "application/json");
//             req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(asset)));
//             try { await req.SendWebRequest(); }
//             catch { return null; }
//             if (req.responseCode != 200) return null;
//             Debug.Log(req.downloadHandler.text);
//             var res = JsonUtility.FromJson<Response<Asset>>(req.downloadHandler.text);
//             if (res.IsError) return null;
//             return res.data;
//         }

//         [System.Serializable]
//         public class UpdateAssetData
//         {
//             [System.NonSerialized] public string server;
//             [System.NonSerialized] public uint worldId;
//             [System.NonSerialized] public uint id;
//             public string url;
//             public string hash;
//             public uint size;
//         }

//         public static async UniTask<Asset> CreateAsset(CreateAssetData asset)
//         {
//             if (User == null) return null;
//             Debug.Log("Creating asset");
//             var config = Config.Load();
//             var gateway = asset.server == User.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(asset.server))?.OriginalString;
//             if (gateway == null) return null;
//             var req = new UnityWebRequest($"{gateway}/api/worlds/{asset.worldId}/assets", "PUT") { downloadHandler = new DownloadHandlerBuffer() };
//             req.SetRequestHeader("Authorization", MostAuth(asset.server));
//             req.SetRequestHeader("Content-Type", "application/json");
//             req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(asset)));
//             try { await req.SendWebRequest(); }
//             catch
//             {
//                 Debug.Log(req.downloadHandler.text);
//                 return null;
//             }
//             Debug.Log(req.downloadHandler.text);
//             if (req.responseCode != 200) return null;
//             var res = JsonUtility.FromJson<Response<Asset>>(req.downloadHandler.text);
//             if (res.IsError) return null;
//             return res.data;
//         }

//         [System.Serializable]
//         public class CreateAssetData
//         {
//             [System.NonSerialized] public string server;
//             [System.NonSerialized] public uint worldId;

//             public string platform;
//             public string engine;
//             public ushort version;
//             public uint id;
//             public string url;
//             public string hash;
//             public uint size;
//         }

//         public static async UniTask<bool> DeleteAsset(string server, uint worldId, uint assetId)
//         {
//             if (User == null) return false;
//             Debug.Log("Deleting asset");
//             var config = Config.Load();
//             var gateway = server == User.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
//             if (gateway == null) return false;
//             var req = new UnityWebRequest($"{gateway}/api/worlds/{worldId}/assets/{assetId}", "DELETE") { downloadHandler = new DownloadHandlerBuffer() };
//             req.SetRequestHeader("Authorization", MostAuth(server));
//             try { await req.SendWebRequest(); }
//             catch { return false; }
//             if (req.responseCode != 200) return false;
//             return true;
//         }

//         public static async UniTask<bool> DeleteWorld(string server, uint worldId)
//         {
//             if (User == null) return false;
//             Debug.Log("Deleting world");
//             var config = Config.Load();
//             var gateway = server == User.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
//             if (gateway == null) return false;
//             var req = new UnityWebRequest($"{gateway}/api/worlds/{worldId}", "DELETE") { downloadHandler = new DownloadHandlerBuffer() };
//             req.SetRequestHeader("Authorization", MostAuth(server));
//             try { await req.SendWebRequest(); }
//             catch { return false; }
//             if (req.responseCode != 200) return false;
//             return true;
//         }

//         public static async UniTask<bool> UploadAssetFile(string server, uint worldId, uint assetId, string path)
//         {
//             if (User == null) return false;
//             Debug.Log("Uploading asset file");
//             var config = Config.Load();
//             var gateway = server == User.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
//             if (gateway == null) return false;
//             byte[] fileBytes = await ReadFileAsync(path);
//             var form = new WWWForm();
//             form.AddBinaryData("file", fileBytes, Path.GetFileName(path));
//             var req = UnityWebRequest.Post($"{gateway}/api/worlds/{worldId}/assets/{assetId}/file", form);
//             req.SetRequestHeader("Authorization", MostAuth(server));
//             req.SetRequestHeader("X-File-Hash", Hashing.HashFile(path));
//             try { await req.SendWebRequest(); }
//             catch
//             {
//                 Debug.Log(req.downloadHandler.text);
//                 return false;
//             }
//             if (req.responseCode != 200) return false;
//             return true;
//         }

//         public static async UniTask<bool> UploadWorldThumbnail(string server, uint worldId, string path)
//         {
//             if (User == null) return false;
//             Debug.Log("Uploading world thumbnail");
//             var config = Config.Load();
//             var gateway = server == User.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
//             if (gateway == null) return false;
//             byte[] fileBytes = await ReadFileAsync(path);
//             var form = new WWWForm();
//             form.AddBinaryData("file", fileBytes, Path.GetFileName(path));
//             var req = UnityWebRequest.Post($"{gateway}/api/worlds/{worldId}/thumbnail", form);
//             req.SetRequestHeader("Authorization", MostAuth(server));
//             req.SetRequestHeader("X-File-Hash", Hashing.HashFile(path));
//             try { await req.SendWebRequest(); }
//             catch { return false; }
//             if (req.responseCode != 200) return false;
//             return true;
//         }

//         async static UniTask<byte[]> ReadFileAsync(string path)
//         {
//             using FileStream sourceStream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
//             byte[] buffer = new byte[sourceStream.Length];
//             await sourceStream.ReadAsync(buffer, 0, (int)sourceStream.Length);
//             return buffer;
//         }
//     }
// }
// #endif