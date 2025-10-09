using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.Panels;

namespace api.nox.user
{
    public class EditorUser : IEditorModInitializer
    {
        internal static EditorModCoreAPI CoreAPI;
        private AuthPanel _auth;
        private ProfilePanel _profile;
        private static EditorPanel _authPanel;
        private static EditorPanel _profilePanel;

        public void OnInitializeEditor(EditorModCoreAPI api)
        {
            CoreAPI = api;
            
            _auth = new AuthPanel();
            _profile = new ProfilePanel();

            _authPanel = api.PanelAPI.AddLocalPanel(_auth);
            _profilePanel = api.PanelAPI.AddLocalPanel(_profile);
        }

        public void OnDisposeEditor()
        {
            CoreAPI.PanelAPI.RemoveLocalPanel(_authPanel);
            CoreAPI.PanelAPI.RemoveLocalPanel(_profilePanel);
            _auth = null;
            _profile = null;
            _authPanel = null;
            _profilePanel = null;
            CoreAPI = null;
        }
    }
}