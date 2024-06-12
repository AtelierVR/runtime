#if UNITY_EDITOR
using System.Collections.Generic;
using Nox.CCK.Editor;
using Nox.CCK.Users;
using UnityEngine;
using UnityEngine.UIElements;

namespace api.nox.user
{
    public class LoginPanel : EditorPanelBuilder
    {
        private UserEditorMod _mod;

        public LoginPanel(UserEditorMod mod)
        {
            _mod = mod;
            root = null;
        }

        public string Id => "main";
        public string Name => "User/Infos";
        public bool Hidded => false;


        public VisualElement root;

        public VisualElement OnOpenned(Dictionary<string, object> data)
        {
            if (root == null)
            {
                root = _mod._api.AssetAPI.GetLocalAsset<VisualTreeAsset>("panel").CloneTree();
                var login = root.Q<VisualElement>("login");
                var loginButton = login.Q<Button>("connect");
                loginButton.clicked += OnClickLogin;
                var current = root.Q<VisualElement>("current");
                var logoutButton = current.Q<Button>("disconnect");
                logoutButton.clicked += OnClickLogout;
                root.Q<Label>("version").text = "v" + _mod._api.ModMetadata.GetVersion();
            }
            Debug.Log("UserMod opened!" + _mod._api.NetworkAPI.GetCurrentUser());
            UpdateUser(_mod._api.NetworkAPI.GetCurrentUser());
            return root;
        }

        public void UpdateUser(User user)
        {
            if (root == null) return;
            var login = root.Q<VisualElement>("login");
            var current = root.Q<VisualElement>("current");
            if (user != null)
            {
                current.Q<TextField>("username").value = user.username;
                current.Q<TextField>("display").value = user.display;
                current.Q<TextField>("id").value = user.id + "@" + user.server;
                var taglist = current.Q<ListView>("tags");
                taglist.makeItem = () =>
                {
                    var label = new Label();
                    label.style.marginLeft = 4;
                    label.style.marginRight = 4;
                    return label;
                };
                taglist.bindItem = (e, i) => (e as Label).text = user.tags[i];
                taglist.itemsSource = user.tags;
                taglist.allowAdd = false;
                taglist.allowRemove = false;
                current.style.display = DisplayStyle.Flex;
                login.style.display = DisplayStyle.None;
            }
            else
            {
                login.style.display = DisplayStyle.Flex;
                current.style.display = DisplayStyle.None;
            }
        }

        private async void OnClickLogin()
        {
            if (root == null) return;
            var login = root.Q<VisualElement>("login");
            var loginIdentifier = login.Q<TextField>("identifier");
            var loginPassword = login.Q<TextField>("password");
            var loginServer = login.Q<TextField>("server");
            var loginform = login.Q<VisualElement>("form");
            var loginError = login.Q<VisualElement>("error");
            var loginErrorMessage = login.Q<Label>("error-message");
            loginform.SetEnabled(false);
            var result = await _mod._api.NetworkAPI.UserAPI.FetchLogin(loginServer.value, loginIdentifier.value, loginPassword.value);
            loginform.SetEnabled(true);
            if (result == null || result.error?.code > 0)
            {
                loginErrorMessage.text = result?.error?.message ?? "An error occured.";
                loginError.style.display = DisplayStyle.Flex;
                UpdateUser(null);
                return;
            }
            else
            {
                loginError.style.display = DisplayStyle.None;
                UpdateUser(result.data.user);
            }
        }

        private async void OnClickLogout()
        {
            var current = root.Q<VisualElement>("current");
            var logoutButton = current.Q<Button>("disconnect");
            logoutButton.SetEnabled(false);
            var result = await _mod._api.NetworkAPI.UserAPI.FetchLogout();
            logoutButton.SetEnabled(true);
            if (result == null || result.error?.code > 0)
            {
                Debug.LogError(result?.error?.message ?? "An error occured.");
                return;
            }
            else UpdateUser(null);
        }
    }
}
#endif