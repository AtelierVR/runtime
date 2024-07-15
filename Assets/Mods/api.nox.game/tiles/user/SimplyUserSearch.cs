using Nox.CCK.Mods;

namespace api.nox.game
{
    public class SimplyUserSearch : ShareObject
    {
        public SimplyUser[] users;
        [ShareObjectImport] public uint total;
        [ShareObjectImport] public uint limit;
        [ShareObjectImport] public uint offset;

        
        [ShareObjectImport] public ShareObject[] SharedUsers;

        public void BeforeImport()
        {
            users = null;
        }

        public void AfterImport()
        {
            users = new SimplyUser[SharedUsers.Length];
            for (int i = 0; i < SharedUsers.Length; i++)
                users[i] = SharedUsers[i].Convert<SimplyUser>();
        }
    }
}