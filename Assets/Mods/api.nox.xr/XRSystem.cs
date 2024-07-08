using System.Collections.Generic;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.XR;
using UnityEngine.XR;

namespace api.nox.xr
{
    public class XRSystem : XRAPI, ModInitializer
    {
        internal ModCoreAPI _api;
        public void OnInitialize(ModCoreAPI api)
        {
            _api = api;
        }

        public void OnUpdate()
        {
        }

        public void OnDispose()
        {
        }


        public InputDevice[] GetDevices()
        {
            List<InputDevice> devices = new();
            InputDevices.GetDevices(devices);
            return devices.ToArray();
        }

        public bool IsEnabled() => XRSettings.enabled && XRSettings.isDeviceActive;
        public InputDevice GetTracker() => InputDevices.GetDeviceAtXRNode(XRNode.TrackingReference);
        public InputDevice GetLeftHand() => InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        public InputDevice GetRightHand() => InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        public void SetEnabled(bool enabled) =>  XRSettings.enabled = enabled;
    }
}