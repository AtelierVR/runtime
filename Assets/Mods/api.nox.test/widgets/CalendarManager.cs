using Nox.CCK;
using Nox.CCK.Mods.Cores;
using UnityEngine;

namespace api.nox.test {
    public class CalendarManager {
        private ClientModCoreAPI api;
        private Widget _widget;

        public CalendarManager(ClientModCoreAPI api) {
            this.api = api;
            GenerateWidget();
            UpdateWidget();
        }

        private void GenerateWidget() {
            _widget = new Widget {
                id = "api.nox.test.calendar",
                width = 4,
                height = 2,
                GetContent = (int o, Transform tf) => GenerateWidgetContent(_widget, tf),
            };
        }

        private GameObject GenerateWidgetContent(Widget data, Transform parent) {
            var baseprefab = api.AssetAPI.GetAsset<GameObject>("game", "prefabs/widget");
            var prefab = api.AssetAPI.GetLocalAsset<GameObject>("prefabs/calendar");
            var btn = Object.Instantiate(baseprefab, parent);
            Object.Instantiate(prefab, Reference.GetReference("content", btn).transform);
            return btn;
        }

        private void UpdateWidget() {
            if (_widget == null) return;
            api.EventAPI.Emit("game.widget", _widget);
        }
        
        public void OnDispose() {
            _widget = null;
        }
    }
}