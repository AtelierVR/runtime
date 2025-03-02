using System.Linq;
using api.nox.network;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;

namespace api.nox.world
{
    public class WorldSystem : MainModInitializer
    {
        internal static WorldSystem Instance;
        private static ModCoreAPI _coreAPI;

        internal NetworkSystem NetworkAPI =>
            _coreAPI.ModAPI.GetMod("network").GetMains().FirstOrDefault() as NetworkSystem;

        public void OnInitialize(ModCoreAPI api)
        {
            _coreAPI = api;
            Instance = this;
        }
        
        public void OnDispose()
        {
            _coreAPI = null;
            Instance = null;
        }
    }
}