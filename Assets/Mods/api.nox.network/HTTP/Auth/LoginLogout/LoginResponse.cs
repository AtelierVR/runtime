using api.nox.network.Users;

namespace api.nox.network.Auths
{
    [System.Serializable]
    public class LoginResponse
    {
        public string error;
        public string token;
        public ulong expires;
        public UserMe user;
    }
}