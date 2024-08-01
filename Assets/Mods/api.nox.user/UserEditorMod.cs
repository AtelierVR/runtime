#if UNITY_EDITOR
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Editor;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.SimplyLibs;
using UnityEngine;
using UnityEngine.UIElements;

namespace api.nox.user
{
    public class UserEditorMod : EditorModInitializer
    {
        internal UserProfilePanel _profile;
        internal UserLoginPanel _login;
        internal EditorPanel __profilepanel;
        internal EditorPanel __loginpanel;
        internal EditorModCoreAPI _api;
        internal SimplyNetworkAPI NetworkAPI => _api.ModAPI.GetMod("network")?.GetMainClasses().OfType<ShareObject>().FirstOrDefault()?.Convert<SimplyNetworkAPI>();

        public void OnInitializeEditor(EditorModCoreAPI api)
        {
            _api = api;
            NetworkAPI.GetCurrentUser();
            _profile = new UserProfilePanel(this);
            __profilepanel = api.PanelAPI.AddLocalPanel(_profile);
            _login = new UserLoginPanel(this);
            __loginpanel = api.PanelAPI.AddLocalPanel(_login);
            GetOrFetchCurrentUser().Forget();
        }

        private async UniTask GetOrFetchCurrentUser()
        {
            var user = NetworkAPI.GetCurrentUser();
            user ??= await NetworkAPI.User.GetMyUser();
        }

        public void OnDispose()
        {

        }

        public void OnUpdateEditor()
        {
            _profile.OnUpdate();
            _login.OnUpdate();
        }
    }
}
#endif