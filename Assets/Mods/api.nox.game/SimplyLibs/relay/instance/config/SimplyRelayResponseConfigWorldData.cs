using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyRelayResponseConfigWorldData : ShareObject
    {
        [ShareObjectImport] public uint MasterId;
        [ShareObjectImport] public string Address;
        [ShareObjectImport] public ushort Version;
    }
}