using Nox.CCK.Mods;

namespace Nox.CCK.Users
{
    [System.Serializable]
    public class UserMe : User
    {
        public string email;
        public uint createdAt;
        public string home;
    }
}