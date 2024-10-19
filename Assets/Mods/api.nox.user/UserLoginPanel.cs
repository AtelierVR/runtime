#if UNITY_EDITOR
using System.Collections.Generic;
using api.nox.network;
using Nox.CCK.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace api.nox.user
{
    public class UserLoginPanel : EditorPanelBuilder
    {
        public string Id { get; } = "login";
        public string Name { get; } = "User/Login";
        public bool Hidded { get => !_mod._profile.Hidded; }
        internal VisualElement _root = new();

        internal UserEditorMod _mod;
        internal UserLoginPanel(UserEditorMod mod) => _mod = mod;

        public void OnClosed()
        {
            Debug.Log("Panel Example closed!");
        }

        internal void OnUpdate() { }

        public VisualElement OnOpenned(Dictionary<string, object> data)
        {
            _root.ClearBindings();
            _root.Clear();
            _root.Add(_mod._api.AssetAPI.GetLocalAsset<VisualTreeAsset>("login").CloneTree());
            _root.Q<Label>("version").text = "v" + _mod._api.ModMetadata.GetVersion();

            var buttonlogin = _root.Q<Button>("login-button");
            var password = _root.Q<TextField>("password");
            var identifier = _root.Q<TextField>("identifier");
            var server = _root.Q<TextField>("server");

            buttonlogin.clickable.clicked += async () =>
            {
                buttonlogin.SetEnabled(false);
                password.isReadOnly = true;
                identifier.isReadOnly = true;
                server.isReadOnly = true;
                var data = await _mod.NetworkAPI.Auth.Login(server.value, identifier.value, password.value);
                buttonlogin.SetEnabled(true);
                password.isReadOnly = false;
                identifier.isReadOnly = false;
                server.isReadOnly = false;
                if (string.IsNullOrEmpty(data.error) && _mod.NetworkAPI.User.CurrentUser != null)
                {
                    _mod._api.PanelAPI.SetActivePanel("api.nox.user.profile");
                    _mod._api.PanelAPI.UpdatePanelList();
                }
                else Debug.Log(data);
            };

            return _root;
        }
    }
}
#endif