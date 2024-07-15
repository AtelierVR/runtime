using Nox.CCK.Mods;

namespace api.nox.network
{

    [System.Serializable]
    public class UserSearch : ShareObject
    {
        public User[] users;
        public uint total;
        public uint limit;
        public uint offset;
    }
}