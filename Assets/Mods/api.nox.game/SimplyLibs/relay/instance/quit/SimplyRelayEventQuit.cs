using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyRelayEventQuit : ShareObject
    {
        public SimplyRelayQuitType Type;
        [ShareObjectImport] public string Reason;
        [ShareObjectImport] public byte SharedType;

        public void AfterImport()
        {
            Type = (SimplyRelayQuitType)SharedType;
            SharedType = 0;
        }
    }
}