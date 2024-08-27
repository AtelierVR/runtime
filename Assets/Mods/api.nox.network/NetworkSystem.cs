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
        internal UserAPI _user;
        internal WorldAPI _world;
        internal ServerAPI _server;
        internal InstanceAPI _instance;
        internal RelayAPI _relays;
        internal AuthAPI _auth;


        public void OnInitialize(ModCoreAPI api)
        {
            _api = api;
            _user = new UserAPI(this);
            _world = new WorldAPI(this);
            _server = new ServerAPI(this);
            _instance = new InstanceAPI(this);
            _relays = new RelayAPI(this);
            _auth = new AuthAPI(this);
        }

        public void OnUpdate()
        {
            _relays.Update();
        }

        public void OnDispose()
        {
            _relays.Dispose();
            _instance.Dispose();
            _user = null;
            _world = null;
            _server = null;
            _instance = null;
            _relays = null;
            _auth = null;
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
                    Debug.Log($"Downloading {url} {req.downloadProgress * 100}%");
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

        public UserMe GetCurrentUser()
        {
            if (_user.user == null) return null;
            _user.user.netSystem = this;
            return _user.user;
        }
        [ShareObjectExport] public Func<ShareObject> GetSharedCurrentUser;
        public Server GetCurrentServer() => _server.server;
        [ShareObjectExport] public Func<ShareObject> GetSharedCurrentServer;
        [ShareObjectExport] public InstanceAPI Instance;
        [ShareObjectExport] public WorldAPI World;
        [ShareObjectExport] public ServerAPI Server;
        [ShareObjectExport] public UserAPI User;
        [ShareObjectExport] public RelayAPI Relay;
        [ShareObjectExport] public AuthAPI Auth;
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
            Relay = _relays;
            Auth = _auth;
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
            Relay = null;
            Auth = null;
        }
    }
}