using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplySearchInstanceData : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public string server;
        [ShareObjectImport, ShareObjectExport] public string query;
        [ShareObjectImport, ShareObjectExport] public string world;
        [ShareObjectImport, ShareObjectExport] public string owner;
        [ShareObjectImport, ShareObjectExport] public uint offset;
        [ShareObjectImport, ShareObjectExport] public uint limit;
    }
}