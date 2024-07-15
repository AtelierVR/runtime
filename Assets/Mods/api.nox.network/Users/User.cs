using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class User : ShareObject
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
    }
}