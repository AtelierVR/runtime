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

    [System.Serializable]
    public class UserUpdate : ShareObject
    {
        [ShareObjectImport] public string username;
        [ShareObjectImport] public string display;
        [ShareObjectImport] public string email;
        [ShareObjectImport] public string password;
        [ShareObjectImport] public string thumbnail;
        [ShareObjectImport] public string banner;
        [ShareObjectImport] public string[] links;
        [ShareObjectImport] public string home;
    }
}