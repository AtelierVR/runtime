using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;

namespace src.editor {
	public class Editor : EditorModInitializer {
		internal static EditorModCoreAPI CoreAPI;
		private         LanguagePack     _lang;

		public void OnInitializeEditor(EditorModCoreAPI api) {
			CoreAPI = api;
			_lang   = api.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_lang);
		}

		public void OnDisposeEditor() {
			LanguageManager.RemovePack(_lang);
			_lang = null;
			CoreAPI = null;
		}
	}
}