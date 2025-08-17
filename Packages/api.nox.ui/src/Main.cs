using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;

namespace api.nox.ui {
	public class Main : MainModInitializer {
		private       LanguagePack   _lang;
		public        MainModCoreAPI CoreAPI;
		public static Main           Instance;

		public void OnInitializeMain(MainModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;
			_lang    = api.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_lang);
		}

		public void OnDisposeMain() {
			LanguageManager.RemovePack(_lang);
			_lang    = null;
			CoreAPI  = null;
			Instance = null;
		}
	}
}