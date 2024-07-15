using Nox.CCK.Mods;

namespace api.nox.game
{
    public class SimplyUserMe : SimplyUser
    {
        [ShareObjectImport, ShareObjectExport] public string email;
        [ShareObjectImport, ShareObjectExport] public uint createdAt;
        [ShareObjectImport, ShareObjectExport] public string home;
    }
}