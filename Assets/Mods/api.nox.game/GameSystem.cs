using Nox.CCK;
using Nox.CCK.Mods;
using UnityEngine;

namespace api.nox.game
{
    public class GameSystem : Nox.CCK.Mods.Initializers.ModInitializer
    {
        private LanguagePack langpack;
        public void OnInitialize(Nox.CCK.Mods.Cores.ModCoreAPI api)
        {
            langpack = api.AssetAPI.GetLocalAsset<LanguagePack>("langpack");
            LanguageManager.LanguagePacks.Add(langpack);
        }

        public void OnDispose()
        {
            LanguageManager.LanguagePacks.Remove(langpack);
            langpack = null;
        }
    }
}