using System;
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

        [ShareObjectExport]
        public Func<string, UniTask<Texture2D>> FetchTexture = async url =>
        {
            var req = new UnityWebRequest(url, "GET") { downloadHandler = new DownloadHandlerTexture() };
            try { await req.SendWebRequest(); } catch { }
            if (req.responseCode != 200) return null;
            return DownloadHandlerTexture.GetContent(req);
        };

        public UserMe GetCurrentUser() => _user.user;
        [ShareObjectExport] public Func<ShareObject> GetSharedCurrentUser;
        public Server GetCurrentServer() => _server.server;
        [ShareObjectExport] public Func<ShareObject> GetSharedCurrentServer;
        [ShareObjectExport] public NetInstance Instance;
        [ShareObjectExport] public NetWorld World;
        [ShareObjectExport] public NetServer Server;
        [ShareObjectExport] public NetUser User;

        public void BeforeExport()
        {
            GetSharedCurrentUser = () => GetCurrentUser();
            GetSharedCurrentServer = () => GetCurrentServer();
            Instance = _instance;
            World = _world;
            Server = _server;
            User = _user;
        }

        public void AfterExport()
        {
            GetSharedCurrentUser = null;
            GetSharedCurrentServer = null;
            Instance = null;
            World = null;
            Server = null;
            User = null;
        }
    }
}