using System.Linq;
using api.nox.world.search;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;

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
        
        internal static INoxObject WorldAPI
            => NetworkAPI.GetField("World");

        private LanguagePack _lang;
        private WorldSearch _worldSearch;

        public void OnInitialize(ModCoreAPI api)
        {
            CoreAPI = api;
            Instance = this;
            _worldSearch = new WorldSearch();
            _lang = CoreAPI.AssetAPI.GetAsset<LanguagePack>("lang.asset");
            LanguageManager.AddPack(_lang);
        }

        public void OnDispose()
        {
            LanguageManager.RemovePack(_lang);
            _worldSearch?.Dispose();
            _worldSearch = null;
            CoreAPI = null;
            Instance = null;
        }
    }
}