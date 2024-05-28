#if UNITY_EDITOR
using Cysharp.Threading.Tasks;
using Nox.CCK.Editor;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine;

namespace api.nox.user
{
    public class UserEditorMod : EditorModInitializer
    {
        internal EditorModCoreAPI _api;
        private LoginPanel _login;
        private EditorPanel _loginpanel;

        public void OnInitializeEditor(EditorModCoreAPI api)
        {
            Debug.Log("UserEditorMod." +  api + ".");
            _api = api;
            _login = new LoginPanel(this);
            _loginpanel = _api.PanelAPI.AddLocalPanel(_login);

            Run().Forget();
        }
        public async UniTask Run()
        {
            Debug.Log("UserEditorMod initialized." +  _api.NetworkAPI + ".");
            var user = await _api.NetworkAPI.UserAPI.FetchUserMe();
            Debug.Log("UserEditorMod initialized 1." +  user + ".");
            _login.UpdateUser(user);
        }

        public void OnDispose()
        {

        }

        public void OnUpdateEditor()
        {
        }
    }
}
#endif