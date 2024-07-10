using Nox.CCK;
using Nox.CCK.Mods.Cores;
using UnityEngine;

namespace api.nox.test {
    public class ExperimentalManager {
        private ClientModCoreAPI api;
        private Widget _widget;

        public ExperimentalManager(ClientModCoreAPI api) {
            this.api = api;
            GenerateWidget();
            UpdateWidget();
        }

        private void GenerateWidget() {
            _widget = new Widget {
                id = "api.nox.test.experimental",
                width = 1,
                height = 1,
                GetContent = (Transform tf) => GenerateWidgetContent(_widget, tf)
            };
        }

        private GameObject GenerateWidgetContent(Widget data, Transform parent) {
            var baseprefab = api.AssetAPI.GetAsset<GameObject>("game", "prefabs/widget");
            var prefab = api.AssetAPI.GetLocalAsset<GameObject>("prefabs/experimental");
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