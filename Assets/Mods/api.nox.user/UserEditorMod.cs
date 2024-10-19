#if UNITY_EDITOR
using System.Linq;
using api.nox.network;
using Cysharp.Threading.Tasks;
using Nox.CCK.Editor;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;

namespace api.nox.user
{
    public class UserEditorMod : EditorModInitializer
    {
        internal UserProfilePanel _profile;
        internal UserLoginPanel _login;
        internal EditorPanel __profilepanel;
        internal EditorPanel __loginpanel;
        internal EditorModCoreAPI _api;

        internal NetworkSystem NetworkAPI => _api.ModAPI.GetMod("network")?.GetMainClasses().OfType<NetworkSystem>().FirstOrDefault();

        public void OnInitializeEditor(EditorModCoreAPI api)
        {
            _api = api;
            _profile = new UserProfilePanel(this);
            __profilepanel = api.PanelAPI.AddLocalPanel(_profile);
            _login = new UserLoginPanel(this);
            __loginpanel = api.PanelAPI.AddLocalPanel(_login);
            GetOrFetchCurrentUser().Forget();
        }

        private async UniTask GetOrFetchCurrentUser()
        {
            var user = NetworkAPI.User.CurrentUser;
            user ??= await NetworkAPI.User.GetMyUser();
        }

        public void OnDispose() { 
            __profilepanel = null;
            __loginpanel = null;
            _profile = null;
            _login = null;
            _api = null;
        }

        public void OnUpdateEditor()
        {
            _profile.OnUpdate();
            _login.OnUpdate();
        }
    }
}
#endif