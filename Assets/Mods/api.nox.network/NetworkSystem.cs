using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Networking;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.network
{
    public class NetworkSystem : MainModInitializer
    {
        internal static ModCoreAPI CoreAPI;
        internal static NetworkSystem ModInstance;
        private LanguagePack _language;

        [NoxPublic(NoxAccess.Read)] public Instances.InstanceAPI Instance;
        [NoxPublic(NoxAccess.Read)] public Auths.AuthAPI Auth;
        [NoxPublic(NoxAccess.Read)] public Users.UserAPI User;
        [NoxPublic(NoxAccess.Read)] public Servers.ServerAPI Server;
        [NoxPublic(NoxAccess.Read)] public Worlds.WorldAPI World;
        [NoxPublic(NoxAccess.Read)] public WebSockets.WebSocketAPI WebSocket;


        public void OnInitialize(ModCoreAPI api)
        {
            CoreAPI = api;
            ModInstance = this;
            NetCache.Clear();
            _language = CoreAPI.AssetAPI.GetAsset<LanguagePack>("langpack.asset");
            LanguageManager.AddPack(_language);

            Auth = new Auths.AuthAPI();
            Instance = new Instances.InstanceAPI();
            Server = new Servers.ServerAPI();
            WebSocket = new WebSockets.WebSocketAPI();
            User = new Users.UserAPI();
            World = new Worlds.WorldAPI();
            // Relay = new Relays.RelayAPI();

            Logger.Log("NetworkSystem initialized");
        }

        public void OnDispose()
        {
            World.Dispose();
            WebSocket.Dispose();
            NetCache.Clear();
            LanguageManager.RemovePack(_language);
            _language = null;
            User = null;
            World = null;
            Server = null;
            Instance = null;
            Auth = null;
            WebSocket = null;
            ModInstance = null;

            Logger.Log("NetworkSystem disposed");
        }

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<Texture2D> FetchTexture(string url, UnityWebRequest req = null,
            Action<float, ulong> progress = null, CancellationToken token = default)
        {
            Logger.Log($"Fetching [TEXTURE] {url}...");
            req ??= new UnityWebRequest(url, "GET");
            req.url = url;
            var dt = new DownloadHandlerTexture();
            req.downloadHandler = dt;
            try
            {
                var asc = req.SendWebRequest();
                await UniTask.WaitUntil(() =>
                {
                    CoreAPI.EventAPI.Emit(new NetEventContext(
                        "network.download",
                        url,
                        req.downloadProgress,
                        req.downloadedBytes)
                    );
                    progress?.Invoke(req.downloadProgress, req.downloadedBytes);
                    return asc.isDone || token.IsCancellationRequested;
                }, cancellationToken: token);
            }
            catch
            {
                // ignored
            }

            return req.responseCode != 200 ? null : dt.texture;
        }

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<string> DownloadFile(string url, string hash, UnityWebRequest req = null,
            Action<float, ulong> progress = null, CancellationToken token = default)
        {
            Logger.Log($"Fetching [FILE] {url}...");
            req ??= new UnityWebRequest(url, "GET");
            req.url = url;
            req.downloadHandler = new DownloadHandlerBuffer();
            try
            {
                var asc = req.SendWebRequest();
                await UniTask.WaitUntil(() =>
                {
                    CoreAPI.EventAPI.Emit(new NetEventContext(
                        "network.download",
                        url,
                        req.downloadProgress,
                        req.downloadedBytes)
                    );
                    progress?.Invoke(req.downloadProgress, req.downloadedBytes);
                    return asc.isDone || token.IsCancellationRequested;
                }, cancellationToken: token);

                if (token.IsCancellationRequested)
                {
                    req.Abort();
                    return null;
                }
            }
            catch
            {
                return null;
            }

            if (req.responseCode != 200) return null;
            var file = Path.Combine(Application.temporaryCachePath, hash);
            if (!Directory.Exists(Path.GetDirectoryName(file)))
                Directory.CreateDirectory(Path.GetDirectoryName(file) ?? string.Empty);
            await File.WriteAllBytesAsync(file, req.downloadHandler.data, token);
            if (Hashing.HashFile(file) == hash) return file;
            File.Delete(file);
            return null;
        }
    }
}