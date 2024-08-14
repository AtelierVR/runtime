using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyCreateWorldData : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public string server;
        [ShareObjectImport, ShareObjectExport] public uint id;
        [ShareObjectImport, ShareObjectExport] public string title;
        [ShareObjectImport, ShareObjectExport] public string description;
        [ShareObjectImport, ShareObjectExport] public ushort capacity;
        [ShareObjectImport, ShareObjectExport] public string thumbnail;
        [ShareObjectImport, ShareObjectExport] public bool custom_id;
    }
}