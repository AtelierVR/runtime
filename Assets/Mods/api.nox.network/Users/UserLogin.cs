using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class UserLogin : ShareObject
    {
        public string error;
        public string token;
        public UserMe user;
    }
}