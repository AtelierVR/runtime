using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyUserMe : SimplyUser
    {
        [ShareObjectImport, ShareObjectExport] public string email;
        [ShareObjectImport, ShareObjectExport] public uint createdAt;
        [ShareObjectImport, ShareObjectExport] public string home;

        public override string ToString() => $"{GetType().Name}[username={username}, display={display}]";
    }
}