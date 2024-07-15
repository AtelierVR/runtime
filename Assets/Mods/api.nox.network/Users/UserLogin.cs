using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class UserLogin : ShareObject
    {
        [ShareObjectExport] public string error;
        [ShareObjectExport] public string token;
        [ShareObjectExport] public UserMe user;
    }
}