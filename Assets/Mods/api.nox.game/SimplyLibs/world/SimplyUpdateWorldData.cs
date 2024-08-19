using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyUpdateWorldData : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public string server;
        [ShareObjectImport, ShareObjectExport] public uint worldId;
        [ShareObjectImport, ShareObjectExport] public string title;
        [ShareObjectImport, ShareObjectExport] public string description;
        [ShareObjectImport, ShareObjectExport] public ushort capacity;
    }
}