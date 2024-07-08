using UnityEngine.XR;

namespace Nox.CCK.Mods.XR
{
    public interface XRAPI
    {
        public bool IsEnabled();

        public InputDevice[] GetDevices();
        public InputDevice GetTracker();
        public InputDevice GetLeftHand();
        public InputDevice GetRightHand();

        public void SetEnabled(bool enabled);
    }
}