#if UNITY_EDITOR
using System.Linq;
using api.nox.avatar.editor;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.Panels;
using Nox.CCK.Utils;
using Nox.Users;

namespace api.nox.avatar {
	public class Editor : IEditorModInitializer {
		internal static EditorModCoreAPI     CoreAPI;
		private         LanguagePack         _lang;
		internal static AvatarBuilderPanel   Builder;
		internal static AvatarPublisherPanel Publisher;
		private static  EditorPanel          _builderPanel;
		private static  EditorPanel          _publisherPanel;

		public static IUserAPI UserAPI
			=> CoreAPI.ModAPI
				.GetMod("user")
				?.GetInstance<IUserAPI>();

		public void OnInitializeEditor(EditorModCoreAPI api) {
			CoreAPI = api;
			_lang   = api.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_lang);
			Builder         = new AvatarBuilderPanel();
			Publisher       = new AvatarPublisherPanel();
			_builderPanel   = api.PanelAPI.AddLocalPanel(Builder);
			_publisherPanel = api.PanelAPI.AddLocalPanel(Publisher);
		}

		public void OnDisposeEditor() {
			LanguageManager.RemovePack(_lang);
			Builder.Dispose();
			CoreAPI.PanelAPI.RemoveLocalPanel(_builderPanel);
			CoreAPI.PanelAPI.RemoveLocalPanel(_publisherPanel);
			_publisherPanel = null;
			_builderPanel   = null;
			Publisher       = null;
			Builder         = null;
			_lang           = null;
			CoreAPI         = null;
		}

		public void OnUpdateEditor() {
			Builder.Update();
			Publisher.Update();
		}

		internal static bool HasOnePanelOpened()
			=> (_builderPanel != null && _builderPanel.IsActive()) ||
			   (_publisherPanel != null && _publisherPanel.IsActive());
	}
}
#endif