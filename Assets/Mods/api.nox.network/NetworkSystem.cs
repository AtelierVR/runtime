using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.Networks;
using Nox.CCK.Users;
using UnityEngine;
using UnityEngine.Networking;

namespace api.nox.network
{
    public class NetworkSystem : NetworkAPI, ModInitializer
    {
        internal ModCoreAPI _api;
        public NetUser User;
        public NetWorld World;

        public void OnInitialize(ModCoreAPI api)
        {
            _api = api;
            User = new NetUser(this);
            World = new NetWorld(this);
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
            if (_api.NetworkAPI.GetCurrentUser().server == server && config.Has("token"))
                return $"Bearer {config.Get<string>("token")}";
            return null;
        }

        public UserMe GetCurrentUser() => User.user;

        public async UniTask<Texture2D> FetchTexture(string url)
        {
            var req = new UnityWebRequest(url, "GET") { downloadHandler = new DownloadHandlerTexture() };
            try { await req.SendWebRequest(); } catch { }
            if (req.responseCode != 200) return null;
            return DownloadHandlerTexture.GetContent(req);
        }

        public NetworkAPIWorld WorldAPI => World;
        public NetworkAPIUser UserAPI => User;
    }
}