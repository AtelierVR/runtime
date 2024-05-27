using Nox.CCK;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.Networks;
using Nox.CCK.Users;
using UnityEngine;

namespace api.nox.network
{
    public class NetworkSystem : NetworkAPI, ModInitializer
    {
        internal ModCoreAPI _api;
        public NetUser User;
        public NetWorld World;

        public void OnInitialize(ModCoreAPI api)
        {
            Debug.Log("NetworkSystem initialized.");
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

        public User GetCurrentUser() => User.user;
        public NetworkAPIWorld WorldAPI => World;
    }
}