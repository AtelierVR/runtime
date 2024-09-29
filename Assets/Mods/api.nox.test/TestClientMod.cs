using Nox.CCK;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine;
using UnityEngine.XR;

namespace api.nox.test
{
    public class TestClientMod : ClientModInitializer
    {
        private ClientModCoreAPI api;
        private ExperimentalManager expirimental;
        private CalendarManager calendar;

        public void OnInitializeClient(ClientModCoreAPI api)
        {
            this.api = api;
            var devices = api.XRAPI.GetDevices();
            Debug.Log("Devices: " + devices.Length);
            foreach (var device in devices)
                Debug.Log("Device: " + device.name);
            expirimental = new ExperimentalManager(api);
            calendar = new CalendarManager(api);

            // listen all input events
            InputDevices.deviceConnected += (device) => Debug.Log("Device connected: " + device.name);
            InputDevices.deviceDisconnected += (device) => Debug.Log("Device disconnected: " + device.name);
            InputDevices.deviceConfigChanged += (device) => Debug.Log("Device config changed: " + device.name);
        }

        public void OnDispose()
        {
            expirimental.OnDispose();
            calendar.OnDispose();
        }
    }
}