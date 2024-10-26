using System.Linq;
using api.nox.xr;
using Nox.CCK;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine.XR;

namespace api.nox.test
{
    public class TestClientMod : ClientModInitializer
    {
        private ClientModCoreAPI api;
        private ExperimentalManager expirimental;
        private CalendarManager calendar;
        internal XRSystem XRAPI => api.ModAPI.GetMod("xr")?.GetMainClasses().OfType<XRSystem>().FirstOrDefault();

        public void OnInitializeClient(ClientModCoreAPI api)
        {
            this.api = api;
            var devices = XRAPI.GetDevices();
            Logger.Log("Devices: " + devices.Length);
            foreach (var device in devices)
                Logger.Log("Device: " + device.name);
            expirimental = new ExperimentalManager(api);
            calendar = new CalendarManager(api);

            // listen all input events
            InputDevices.deviceConnected += (device) => Logger.Log("Device connected: " + device.name);
            InputDevices.deviceDisconnected += (device) => Logger.Log("Device disconnected: " + device.name);
            InputDevices.deviceConfigChanged += (device) => Logger.Log("Device config changed: " + device.name);
        }

        public void OnDispose()
        {
            expirimental.OnDispose();
            calendar.OnDispose();
        }
    }
}