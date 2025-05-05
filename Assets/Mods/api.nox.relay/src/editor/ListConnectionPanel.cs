#if UNITY_EDITOR
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Panels;
using UnityEngine.UIElements;

namespace api.nox.relay.editor
{
    public class ListConnectionPanel : EditorPanelBuilder
    {
        public string GetId() => "list_connections";
        public string GetName() => "Relay/Connections";
        public bool IsHidden() => false;

        private readonly VisualElement _root = new();


        public VisualElement Make(Dictionary<string, object> data)
        {
            _root.ClearBindings();
            _root.Clear();

            var child = RelayEditor.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("list.uxml").CloneTree();
            child.style.flexGrow = 1;
            _root.Add(child);

            _root.Q<Label>("version").text = "v" + RelayEditor.CoreAPI.ModMetadata.GetVersion();

            var connect = _root.Q<Button>("connection-button");
            connect.RegisterCallback<ClickEvent>(evt => OnClickConnection().Forget());

            return _root;
        }

        public async UniTask OnClickConnection()
        {
            var connect = _root.Q<Button>("connection-button");
            var address = _root.Q<TextField>("connection-address").value;
            if (!connect.enabledSelf) return;
            connect.SetEnabled(false);

            if (string.IsNullOrEmpty(address))
            {
                UnityEditor.EditorUtility.DisplayDialog("Error", "Address is empty", "OK");
                connect.SetEnabled(true);
                return;
            }
            
            
        }
    }
}
#endif