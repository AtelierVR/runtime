using Nox.CCK.Mods;

namespace api.nox.network
{

    [System.Serializable]
    public class UserSearch : ShareObject
    {
        public User[] users;
        [ShareObjectExport] public uint total;
        [ShareObjectExport] public uint limit;
        [ShareObjectExport] public uint offset;

        [ShareObjectExport] public ShareObject[] SharedUsers;

        public void BeforeExport()
        {
            SharedUsers = new ShareObject[users.Length];
            for (int i = 0; i < users.Length; i++)
                SharedUsers[i] = users[i];
        }

        public void AfterExport()
        {
            SharedUsers = null;
        }
    }
}