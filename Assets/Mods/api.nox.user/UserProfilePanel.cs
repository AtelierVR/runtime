#if UNITY_EDITOR
using System.Collections.Generic;
using Nox.CCK.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace api.nox.user
{
    public class UserProfilePanel : EditorPanelBuilder
    {
        public string Id { get; } = "profile";
        public string Name { get; } = "User/Profile";
        public bool Hidded { get => _mod.NetworkAPI.GetCurrentUser() == null; }
        internal VisualElement _root = new();

        internal UserEditorMod _mod;
        internal UserProfilePanel(UserEditorMod mod) => _mod = mod;

        public void OnClosed()
        {
            Debug.Log("Panel Example closed!");
        }

        internal void OnUpdate()
        {

        }

        public VisualElement OnOpenned(Dictionary<string, object> data)
        {
            _root.ClearBindings();
            _root.Clear();
            _root.Add(_mod._api.AssetAPI.GetLocalAsset<VisualTreeAsset>("profile").CloneTree());
            _root.Q<Label>("version").text = "v" + _mod._api.ModMetadata.GetVersion();
            return _root;
        }
    }
}
#endif