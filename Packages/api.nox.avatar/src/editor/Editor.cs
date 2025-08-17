using System.Linq;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Users;

namespace api.nox.avatar {
	public class Editor : EditorModInitializer {
		internal static EditorModCoreAPI CoreAPI;
		private         LanguagePack     _lang;

		public static IUserAPI UserAPI
			=> CoreAPI.ModAPI
				.GetMod("user")
				.GetMains()
				.FirstOrDefault() as IUserAPI;

		public void OnInitializeEditor(EditorModCoreAPI api) {
			CoreAPI = api;
			_lang   = api.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_lang);
			Layers.CreateLayers(new[] { Avatar.LocalLayer, Avatar.RemoteLayer });
		}

		public void OnDisposeEditor() {
			LanguageManager.RemovePack(_lang);
			_lang   = null;
			CoreAPI = null;
		}
	}
}