using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyCreateAssetData : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public string server;
        [ShareObjectImport, ShareObjectExport] public uint worldId;
        
        [ShareObjectImport, ShareObjectExport] public uint id;
        [ShareObjectImport, ShareObjectExport] public ushort version;
        [ShareObjectImport, ShareObjectExport] public string engine;
        [ShareObjectImport, ShareObjectExport] public string platform;
        [ShareObjectImport, ShareObjectExport] public string url;
        [ShareObjectImport, ShareObjectExport] public string hash;
        [ShareObjectImport, ShareObjectExport] public uint size;
    }
}