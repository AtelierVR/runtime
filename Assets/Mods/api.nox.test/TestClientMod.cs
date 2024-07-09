using System;
using Nox.CCK;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace api.nox.test
{
    public class TestClientMod : ClientModInitializer
    {
        private ClientModCoreAPI api;
        public void OnInitializeClient(ClientModCoreAPI api)
        {
            this.api = api;

            Debug.Log("Hello from TestClientMod!");

            Debug.Log("API: " + api.XRAPI);

            var mods = api.ModAPI.GetMods();
            Debug.Log("Mods: " + mods.Length);
            foreach (var mod in mods)
                Debug.Log("Mod: " + mod.GetMetadata().GetId());
            // Example of using the XR API
            var devices = api.XRAPI.GetDevices();

            Debug.Log("Devices: " + devices.Length);
            foreach (var device in devices)
                Debug.Log("Device: " + device.name);

            GenerateWidget();
            UpdateWidget();
        }

        private Widget _widget;

        private void GenerateWidget()
        {
            _widget = new Widget
            {
                id = "api.nox.test.widget",
                width = 1,
                height = 1,
                GetContent = (Transform tf) => GenerateWidgetContent(_widget, tf)
            };
        }

        private GameObject GenerateWidgetContent(Widget data, Transform parent)
        {
            var baseprefab = api.AssetAPI.GetAsset<GameObject>("game", "prefabs/widget");
            var prefab = api.AssetAPI.GetLocalAsset<GameObject>("prefabs/widget");
            var btn = Object.Instantiate(baseprefab, parent);
            Object.Instantiate(prefab, Reference.GetReference("content", btn).transform);
            return btn;
        }

        private void UpdateWidget()
        {
            if (_widget == null) return;
            api.EventAPI.Emit("game.widget", _widget);
        }

        private void OnWidgetClicked()
        {
            Debug.Log("Widget clicked");
        }

        private void OnWidgetHover(bool isHovered)
        {
            Debug.Log("Widget hovered: " + isHovered);
        }

        public void OnUpdateClient()
        {

        }
        public void OnDispose()
        {

        }
    }

    public class Widget : ShareObject
    {
        public string id;
        public uint width = 1;
        public uint height = 1;
        public Func<Transform, GameObject> GetContent;
        public bool isInteractable = true;
        public uint weight = 1;
    }
}