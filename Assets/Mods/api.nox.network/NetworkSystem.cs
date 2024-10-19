using System;
using System.IO;
using api.nox.network.HTTP;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine;
using UnityEngine.Networking;

namespace api.nox.network
{
    public class NetworkSystem : ModInitializer
    {
        internal static ModCoreAPI CoreAPI;
        internal static NetworkSystem ModInstance;
        public Instances.InstanceAPI Instance;
        public Auths.AuthAPI Auth;
        public Users.UserAPI User;
        public Servers.ServerAPI Server;
        public Worlds.WorldAPI World;
        public WebSockets.WebSocketAPI WebSocket;

        public Relays.RelayAPI Relay;


        public void OnInitialize(ModCoreAPI api)
        {
            CoreAPI = api;
            ModInstance = this;
            NetCache.Clear();

            Auth = new Auths.AuthAPI();
            Instance = new Instances.InstanceAPI();
            Server = new Servers.ServerAPI();
            WebSocket = new WebSockets.WebSocketAPI();
            User = new Users.UserAPI();
            World = new Worlds.WorldAPI();
            Relay = new Relays.RelayAPI();

            Debug.Log("NetworkSystem initialized");
        }

        public void OnUpdate()
        {
            Relay.Update();
        }

        public void OnDispose()
        {
            Relay.Dispose();
            World.Dispose();
            WebSocket.Dispose();
            NetCache.Clear();

            User = null;
            World = null;
            Server = null;
            Instance = null;
            Relay = null;
            Auth = null;
            WebSocket = null;
            ModInstance = null;

            Debug.Log("NetworkSystem disposed");
        }

        public async UniTask<Texture2D> FetchTexture(string url, UnityWebRequest req = null)
        {
            Debug.Log($"Fetching [TEXTURE] {url}...");
            req ??= new UnityWebRequest(url, "GET");
            req.url = url;
            var dt = new DownloadHandlerTexture();
            req.downloadHandler = dt;
            try
            {
                var asynco = req.SendWebRequest();
                await UniTask.WaitUntil(() =>
                {
                    CoreAPI.EventAPI.Emit(new NetEventContext("network.download", new { url, progress = req.downloadProgress }, true));
                    return asynco.isDone;
                });
            }
            catch { }
            if (req.responseCode != 200) return null;
            return dt.texture;
        }

        public async UniTask<string> DownloadFile(string url, string hash, UnityWebRequest req = null)
        {
            Debug.Log($"Fetching [FILE] {url}...");
            req ??= new UnityWebRequest(url, "GET");
            req.url = url;
            req.downloadHandler = new DownloadHandlerBuffer();
            try
            {
                var asynco = req.SendWebRequest();
                await UniTask.WaitUntil(() =>
                {
                    Debug.Log($"Downloading {url} {req.downloadProgress * 100}%");
                    CoreAPI.EventAPI.Emit(new NetEventContext("network.download", url, req.downloadProgress, req.downloadedBytes));
                    return asynco.isDone;
                });
            }
            catch { return null; }
            if (req.responseCode != 200) return null;
            var file = Path.Combine(Application.temporaryCachePath, hash);
            if (!Directory.Exists(Path.GetDirectoryName(file)))
                Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllBytes(file, req.downloadHandler.data);
            if (Hashing.HashFile(file) != hash)
            {
                File.Delete(file);
                return null;
            }
            return file;
        }
    }
}