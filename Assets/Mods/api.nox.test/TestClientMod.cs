using Nox.CCK;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine;

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
        }
        
        public void OnDispose()
        {
            expirimental.OnDispose();
            calendar.OnDispose();
        }
    }
}