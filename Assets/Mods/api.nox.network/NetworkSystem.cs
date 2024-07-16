using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine;
using UnityEngine.Networking;

namespace api.nox.network
{
    public class NetworkSystem : ShareObject, ModInitializer
    {
        internal ModCoreAPI _api;
        private NetUser _user;
        private NetWorld _world;
        private NetServer _server;
        private NetInstance _instance;


        public void OnInitialize(ModCoreAPI api)
        {
            _api = api;
            _user = new NetUser(this);
            _world = new NetWorld(this);
            _server = new NetServer(this);
            _instance = new NetInstance(this);
        }

        public void OnUpdate()
        {
        }

        public void OnDispose()
        {
        }

        public string MostAuth(string server)
        {
            var config = Config.Load();
            if (GetCurrentUser()?.server == server && config.Has("token"))
                return $"Bearer {config.Get<string>("token")}";
            return null;
        }

        public bool TryMostAuth(string server, out string auth)
        {
            auth = MostAuth(server);
            return auth != null;
        }

        internal async UniTask<Texture2D> FetchTexture(string url, UnityWebRequest req = null)
        {
            req ??= new UnityWebRequest(url, "GET");
            req.url = url;
            var dt = new DownloadHandlerTexture();
            req.downloadHandler = dt;
            try
            {
                var asynco = req.SendWebRequest();
                await UniTask.WaitUntil(() =>
                {
                    _api.EventAPI.Emit(new NetEventContext("network.download", new { url, progress = req.downloadProgress }, true));
                    return asynco.isDone;
                });
            }
            catch { }
            if (req.responseCode != 200) return null;
            return dt.texture;
        }

        internal async UniTask<string> DownloadFile(string url, string hash, UnityWebRequest req = null)
        {
            req ??= new UnityWebRequest(url, "GET");
            req.url = url;
            req.downloadHandler = new DownloadHandlerBuffer();
            try
            {
                var asynco = req.SendWebRequest();
                await UniTask.WaitUntil(() =>
                {
                    _api.EventAPI.Emit(new NetEventContext("network.download", url, req.downloadProgress, req.downloadedBytes));
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

        public UserMe GetCurrentUser() => _user.user;
        [ShareObjectExport] public Func<ShareObject> GetSharedCurrentUser;
        public Server GetCurrentServer() => _server.server;
        [ShareObjectExport] public Func<ShareObject> GetSharedCurrentServer;
        [ShareObjectExport] public NetInstance Instance;
        [ShareObjectExport] public NetWorld World;
        [ShareObjectExport] public NetServer Server;
        [ShareObjectExport] public NetUser User;
        [ShareObjectExport] public Func<string, UnityWebRequest, UniTask<Texture2D>> SharedFetchTexture;
        [ShareObjectExport] public Func<string, string, UnityWebRequest, UniTask<string>> SharedDownloadFile;

        public void BeforeExport()
        {
            GetSharedCurrentUser = () => GetCurrentUser();
            GetSharedCurrentServer = () => GetCurrentServer();
            SharedFetchTexture = async (url, req) => await FetchTexture(url, req);
            SharedDownloadFile = async (url, hash, req) => await DownloadFile(url, hash, req);
            Instance = _instance;
            World = _world;
            Server = _server;
            User = _user;
        }

        public void AfterExport()
        {
            GetSharedCurrentUser = null;
            GetSharedCurrentServer = null;
            SharedFetchTexture = null;
            SharedDownloadFile = null;
            Instance = null;
            World = null;
            Server = null;
            User = null;
        }
    }
}