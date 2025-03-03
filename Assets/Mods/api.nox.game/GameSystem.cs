using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.game
{
    public class GameSystem : MainModInitializer
    {
        private LanguagePack _langpack;
        internal static GameSystem Instance;
        internal ModCoreAPI CoreAPI;

        public void OnInitialize(ModCoreAPI api)
        {
            Logger.Log("GameSystem initialized");
            CoreAPI = api;
            Instance = this;
            _langpack = api.AssetAPI.GetAsset<LanguagePack>("langpack.asset");
            LanguageManager.AddPack(_langpack);
        }
        

        public void OnDispose()
        {
            LanguageManager.RemovePack(_langpack);
            _langpack = null;
            Instance = null;
        }
    }
}
