using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.Panels;

namespace api.nox.user {
	public class EditorUser : IEditorModInitializer {
		internal static IEditorModCoreAPI      CoreAPI;
		public static   AuthentificationPanel Auth;
		public static   ProfilePanel          Profile;
		private static  IEditorPanel           _authPanel;
		private static  IEditorPanel           _profilePanel;

		public void OnInitializeEditor(IEditorModCoreAPI api) {
			CoreAPI = api;

			Auth    = new AuthentificationPanel();
			Profile = new ProfilePanel();

			_authPanel    = api.PanelAPI.AddLocalPanel(Auth);
			_profilePanel = api.PanelAPI.AddLocalPanel(Profile);
		}

		public void OnDisposeEditor() {
			CoreAPI.PanelAPI.RemoveLocalPanel(_authPanel);
			CoreAPI.PanelAPI.RemoveLocalPanel(_profilePanel);
			Auth          = null;
			Profile       = null;
			_authPanel    = null;
			_profilePanel = null;
			CoreAPI       = null;
		}
	}
}