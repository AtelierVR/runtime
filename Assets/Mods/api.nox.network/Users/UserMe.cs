using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class UserMe : ShareObject
    {
        [ShareObjectExport] public uint id;
        [ShareObjectExport] public string username;
        [ShareObjectExport] public string display;
        [ShareObjectExport] public string[] tags;
        [ShareObjectExport] public string server;
        [ShareObjectExport] public float rank;
        [ShareObjectExport] public string[] links;
        [ShareObjectExport] public string banner;
        [ShareObjectExport] public string thumbnail;
        [ShareObjectExport] public string email;
        [ShareObjectExport] public uint createdAt;
        [ShareObjectExport] public string home;
    }
}