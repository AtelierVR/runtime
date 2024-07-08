namespace Nox.CCK.Users
{
    [System.Serializable]
    public class User
    {
        public uint id;
        public string username;
        public string display;
        public string[] tags;
        public string server;
        public float rank;
        public string[] links;
    }
}