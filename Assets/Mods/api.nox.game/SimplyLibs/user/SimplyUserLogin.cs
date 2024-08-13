using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    [System.Serializable]
    public class SimplyUserLogin : ShareObject
    {
        [ShareObjectExport, ShareObjectImport] public string error;
        [ShareObjectExport, ShareObjectImport] public string token;
        public SimplyUserMe user;

        [ShareObjectExport, ShareObjectImport] public ShareObject SharedUser;

        public void AfterImport()
        {
            user = SharedUser?.Convert<SimplyUserMe>();
            SharedUser = null;
        }

        public override string ToString() => $"{GetType().Name}[error={error}, token={token}, user={user}]";
    }
}