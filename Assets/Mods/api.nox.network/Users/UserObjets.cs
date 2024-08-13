using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class UserLogin : ShareObject
    {
        [ShareObjectExport] public string error;
        [ShareObjectExport] public string token;
        public UserMe user;

        [ShareObjectExport, ShareObjectImport] public ShareObject SharedUser;

        public void BeforeExport()
        {
            SharedUser = user;
        }

        public void AfterImport()
        {
            SharedUser = null;
        }
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

    [System.Serializable]
    internal class UserLoginRequest
    {
        public string identifier;
        public string password;
    }

    [System.Serializable]
    internal class UserLogout {
        public bool success;
    }
}