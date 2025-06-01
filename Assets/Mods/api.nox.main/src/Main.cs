using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;

namespace api.nox.main {
	public class Main : MainModInitializer {
		private LanguagePack _lang;

		public void OnInitialize(ModCoreAPI api) {
			_lang = api.AssetAPI.GetAsset<LanguagePack>("pack.asset");
			LanguageManager.AddPack(_lang);
		}
		
		public void OnDispose()
			=> LanguageManager.RemovePack(_lang);
	}
}