using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;

namespace api.nox.main {
	public class Main : MainModInitializer {
		private LanguagePack _lang;

		public void OnInitialize(ModCoreAPI api) {
			Logger.LogDebug($"{api.ModMetadata.GetId()}:");
			foreach (var d in api.ModAPI.GetMod(api.ModMetadata.GetId()).GetDatas())
				Logger.LogDebug($"  {d.Key} = {d.Value}");
			
			_lang = api.AssetAPI.GetAsset<LanguagePack>("pack.asset");
			LanguageManager.AddPack(_lang);
			
		}
		
		public void OnDispose()
			=> LanguageManager.RemovePack(_lang);
	}
}