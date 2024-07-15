using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class UserMe : ShareObject
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
        public string email;
        public uint createdAt;
        public string home;
    }
}