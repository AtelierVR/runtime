using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplySearchUserData : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public string server;
        [ShareObjectImport, ShareObjectExport] public string query;
        [ShareObjectImport, ShareObjectExport] public string[] user_ids;
        [ShareObjectImport, ShareObjectExport] public uint offset;
        [ShareObjectImport, ShareObjectExport] public uint limit;
    }
}