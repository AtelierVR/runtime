using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyCreateInstanceData : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public string server;

        [ShareObjectImport, ShareObjectExport] public string name;
        [ShareObjectImport, ShareObjectExport] public string title;
        [ShareObjectImport, ShareObjectExport] public string description;
        [ShareObjectImport, ShareObjectExport] public string expose;
        [ShareObjectImport, ShareObjectExport] public string world;
        [ShareObjectImport, ShareObjectExport] public ushort capacity;
        [ShareObjectImport, ShareObjectExport] public bool use_password;
        [ShareObjectImport, ShareObjectExport] public string password;
        [ShareObjectImport, ShareObjectExport] public bool use_whitelist;
        [ShareObjectImport, ShareObjectExport] public string[] whitelist;
        [ShareObjectImport, ShareObjectExport] public string thumbnail;
    }
}