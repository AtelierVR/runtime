using api.nox.game.keybindings;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.game
{
    public class GameSystem : MainModInitializer
    {
        private LanguagePack _langpack;
        internal static GameSystem Instance;
        internal ModCoreAPI CoreAPI;

        [NoxPublic(NoxAccess.Read)] public KeyBindingManager KeyBindings;

        public void OnInitialize(ModCoreAPI api)
        {
            Logger.Log("GameSystem initialized");
            CoreAPI = api;
            KeyBindings = new KeyBindingManager();
            Instance = this;
            _langpack = api.AssetAPI.GetAsset<LanguagePack>("langpack.asset");
            LanguageManager.AddPack(_langpack);
        }
        

        public void OnDispose()
        {
            LanguageManager.RemovePack(_langpack);
            _langpack = null;
            KeyBindings.Dispose();
            Instance = null;
        }
    }
}
