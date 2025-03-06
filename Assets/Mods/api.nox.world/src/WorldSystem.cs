using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;

namespace api.nox.world
{
    public class WorldSystem : MainModInitializer
    {
        internal static WorldSystem Instance;
        internal static ModCoreAPI CoreAPI;

        internal static MainModInitializer NetworkAPI
            => CoreAPI.ModAPI
                .GetMod("network").GetMains()
                .FirstOrDefault();

        public void OnInitialize(ModCoreAPI api)
        {
            CoreAPI = api;
            Instance = this;
        }

        public void OnDispose()
        {
            CoreAPI = null;
            Instance = null;
        }
    }
}