using System;
using System.Collections.Generic;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Object = UnityEngine.Object;

namespace api.nox.search.widget
{
    public class SearchWidget : IDisposable
    {
        private Dictionary<string, object> _widget;

        private INoxObject WidgetAPI
            => SearchClient.UISystem
                .GetField("Widgets");

        internal SearchWidget()
        {
            SearchSystem.OnHandlerAdded.AddListener(OnUpdated);
            SearchSystem.OnHandlerRemoved.AddListener(OnUpdated);
            OnUpdated(null);
        }

        public void Dispose()
        {
            SearchSystem.OnHandlerAdded.RemoveListener(OnUpdated);
            SearchSystem.OnHandlerRemoved.RemoveListener(OnUpdated);
        }

        private void OnUpdated(Handler handler)
        {
            if (SearchClient.UISystem == null)
            {
                Logger.LogDebug($"UISystem is null");
                return;
            }

            _widget ??= new Dictionary<string, object>
            {
                { "key", "search" },
                { "width", 1 },
                { "height", 1 },
                { "content", null }
            };

            if (SearchSystem.Instance.Handlers.Count == 0 && WidgetAPI.CallMethod<bool>("Has", _widget["key"]))
                WidgetAPI.CallMethod("Remove", _widget["key"]);

            if (SearchSystem.Instance.Handlers.Count == 0)
            {
                Logger.LogDebug($"SearchWidget: No handler, removing widget");
                return;
            }

            _widget["content"] = new Func<int, RectTransform, GameObject>(OnContent);

            WidgetAPI.CallMethod(
                WidgetAPI.CallMethod<bool>("Has", _widget["key"])
                    ? "Change"
                    : "Add",
                _widget
            );
        }

        private GameObject OnContent(int menuId, RectTransform transform)
        {
            var asset = SearchSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("ui", "prefabs/widgets/button.prefab");
            var button = Object.Instantiate(asset, transform);
            var reference = Reference.GetReference("content", button);
            asset = SearchSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/widget.prefab");
            var widget = Object.Instantiate(asset, reference.transform);
            var comportment = widget.GetComponent<SearchWidgetComportment>();
            comportment.button = button.GetComponent<UnityEngine.UI.Button>();
            comportment.menuId = menuId;
            comportment.UpdateContent();
            return button;
        }
    }
}