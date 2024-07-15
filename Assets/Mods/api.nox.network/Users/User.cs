using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class User : ShareObject
    {
        public uint id;
        public string username;
        public string display;
        public string[] tags;
        public string server;
        public float rank;
        public string[] links;
        public string banner;
        public string thumbnail;
    }
}