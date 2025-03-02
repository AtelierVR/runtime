using api.nox.network.Users;
using Nox.CCK.Utils;

namespace api.nox.network.Auths
{
    [System.Serializable]
    public class LoginResponse : INoxObject
    {
        [NoxPublic(NoxAccess.Read)] public string error;
        [NoxPublic(NoxAccess.Read)] public string token;
        [NoxPublic(NoxAccess.Read)] public ulong expires;
        [NoxPublic(NoxAccess.Read)] public UserMe user;
    }
}