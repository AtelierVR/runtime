using Nox.CCK.Mods;

namespace api.nox.game
{
    public class SimplyServer : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public string id;
        [ShareObjectImport, ShareObjectExport] public string title;
        [ShareObjectImport, ShareObjectExport] public string description;
        [ShareObjectImport, ShareObjectExport] public string address;
        [ShareObjectImport, ShareObjectExport] public string version;
        [ShareObjectImport, ShareObjectExport] public long ready_at;
        [ShareObjectImport, ShareObjectExport] public string icon;
        [ShareObjectImport, ShareObjectExport] public string public_key;
    }
}