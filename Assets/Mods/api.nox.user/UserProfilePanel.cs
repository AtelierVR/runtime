#if UNITY_EDITOR
using System.Collections.Generic;
using api.nox.network;
using Cysharp.Threading.Tasks;
using Nox.CCK.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace api.nox.user
{
    public class UserProfilePanel : EditorPanelBuilder
    {
        public string Id { get; } = "profile";
        public string Name { get; } = "User/Profile";
        public bool Hidded { get => _mod.NetworkAPI.User.CurrentUser == null; }
        internal VisualElement _root = new();

        internal UserEditorMod _mod;
        internal UserProfilePanel(UserEditorMod mod) => _mod = mod;

        public void OnClosed()
        {
            Debug.Log("Panel Example closed!");
        }

        internal void OnUpdate() { }

        public VisualElement OnOpenned(Dictionary<string, object> data)
        {
            _root.ClearBindings();
            _root.Clear();
            _root.Add(_mod._api.AssetAPI.GetLocalAsset<VisualTreeAsset>("profile").CloneTree());
            _root.Q<Label>("version").text = "v" + _mod._api.ModMetadata.GetVersion();

            var buttonrefresh = _root.Q<Button>("refresh");
            buttonrefresh.clickable.clicked += async () =>
            {
                await _mod.NetworkAPI.User.GetMyUser();
                UpdateUser();
            };
            UpdateUser();
            var buttonlogout = _root.Q<Button>("logout-button");
            buttonlogout.clickable.clicked += () => UniTask.Create(async () =>
                {
                    buttonlogout.SetEnabled(false);
                    if (await _mod.NetworkAPI.Auth.Logout())
                    {
                        _mod._api.PanelAPI.SetActivePanel("api.nox.user.login");
                        _mod._api.PanelAPI.UpdatePanelList();
                    }
                    else
                    {
                        await _mod.NetworkAPI.User.GetMyUser();
                        UpdateUser();
                        buttonlogout.SetEnabled(true);
                    }
                }).Forget();

            return _root;
        }

        private async UniTask UpdateTexture(string url, Image image)
        {
            var texture = await _mod.NetworkAPI.FetchTexture(url);
            image.image = texture;
        }

        private void UpdateUser()
        {
            var user = _mod.NetworkAPI.User.CurrentUser;
            if (user == null) return;
            _root.Q<TextField>("username").value = user.username;
            _root.Q<TextField>("server").value = user.server;
            _root.Q<TextField>("display").value = user.display;
            _root.Q<UnsignedIntegerField>("id").value = user.id;
            _root.Q<Label>("display_name").text = user.display;

            var div_with_banner = _root.Q<VisualElement>("with-banner");
            var div_without_banner = _root.Q<VisualElement>("without-banner");

            var div = user.banner != null ? div_with_banner : div_without_banner;
            if (!string.IsNullOrEmpty(user.banner))
            {
                var image = div.Q<Image>("banner");
                if (!string.IsNullOrEmpty(user.banner))
                    UpdateTexture(user.banner, image).Forget();
                else image.image = null;
            }

            var image_avatar = div.Q<Image>("thumbnail");
            if (!string.IsNullOrEmpty(user.thumbnail))
                UpdateTexture(user.thumbnail, image_avatar).Forget();
            else image_avatar.image = null;

            div_with_banner.style.display = user.banner != null ? DisplayStyle.Flex : DisplayStyle.None;
            div_without_banner.style.display = user.banner == null ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
#endif