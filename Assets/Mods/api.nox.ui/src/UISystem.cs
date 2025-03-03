using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;

namespace Mods.api.nox.ui
{
    public class UISystem : MainModInitializer
    {
        internal static UISystem Instance;
        internal static MainModCoreAPI CoreAPI;

        public void OnInitializeMain(MainModCoreAPI api)
        {
            Instance = this;
            CoreAPI = api;
        }

        public void OnDisposeMain()
        {
            Instance = null;
            CoreAPI = null;
        }
    }
}