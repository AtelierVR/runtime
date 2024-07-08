using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine;

namespace api.nox.test
{
    public class TestClientMod : ClientModInitializer
    {
        public void OnInitializeClient(ClientModCoreAPI api)
        {
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
        }

        public void OnUpdateClient()
        {

        }
        public void OnDispose()
        {

        }
    }
}