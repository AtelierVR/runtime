using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{

    public class SimplyMakeRelayConnectionResponse : ShareObject
    {
        public SimplyRelay Relay;
        [ShareObjectImport] public bool IsSuccess;
        [ShareObjectImport] public string Error;

        [ShareObjectImport] public ShareObject SharedRelay;

        public void AfterImport()
        {
            Relay = SharedRelay?.Convert<SimplyRelay>();
            SharedRelay = null;
        }
    }
}