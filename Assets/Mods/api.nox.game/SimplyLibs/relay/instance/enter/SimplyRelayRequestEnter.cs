using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyRelayRequestEnter : ShareObject
    {

        public SimplyRelayEnterFlags Flags;
        [ShareObjectImport] public string DisplayName;
        [ShareObjectImport] public string Password;
        [ShareObjectImport] public byte SharedFlags;

        public void AfterImport()
        {
            Flags = (SimplyRelayEnterFlags)SharedFlags;
            SharedFlags = 0;
        }
    }
}