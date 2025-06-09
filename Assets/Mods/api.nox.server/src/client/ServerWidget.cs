using System;
using System.Collections.Generic;
using Nox.CCK.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace api.nox.server
{
    public class ServerWidget : IDisposable
    {
        private Dictionary<string, object> _widget;

        private INoxObject WidgetAPI
            => ServerClient.UISystem
                .GetField("Widgets");

        internal ServerWidget()
        {
            Main.Instance.OnServerUpdated.AddListener(OnServerUpdated);
            OnServerUpdated(Main.ServerAPI.CallMethod("GetCurrentServer"));
        }

        public void Dispose()
        {
            Main.Instance.OnServerUpdated.RemoveListener(OnServerUpdated);
        }

        private void OnServerUpdated(INoxObject server)
        {
            if (ServerClient.UISystem == null) return;

            _widget ??= new Dictionary<string, object>
            {
                { "key", "my_server" },
                { "width", 1 },
                { "height", 1 },
                { "content", null }
            };

            if (server == null && WidgetAPI.CallMethod<bool>("Has", _widget["key"]))
                WidgetAPI.CallMethod("Remove", _widget["key"]);

            if (server == null) return;

            _widget["content"] = new Func<int, RectTransform, GameObject>(OnContent);

            WidgetAPI.CallMethod(
                WidgetAPI.CallMethod<bool>("Has", _widget["key"])
                    ? "Change"
                    : "Add",
                _widget
            );
        }
        
        private GameObject OnContent(int menuId, RectTransform rect)
        {
            var asset = Main.CoreAPI.AssetAPI.GetAsset<GameObject>("ui", "prefabs/widgets/button.prefab");
            var button = Object.Instantiate(asset, rect);
            var reference = Reference.GetReference("content", button);
            asset = Main.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/widget.prefab");
            var widget = Object.Instantiate(asset, reference.transform);
            var comportment = widget.GetComponent<ServerWidgetComportment>();
            comportment.button = button.GetComponent<UnityEngine.UI.Button>();
            comportment.menuId = menuId;
            comportment.UpdateContent(Main.ServerAPI.CallMethod("GetCurrentServer"));
            return button;
        }
    }
}