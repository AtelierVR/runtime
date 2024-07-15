using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class Instance : ShareObject
    {
        public uint id;
        public string title;
        public string description;
        public string server;
        public string name;
        public ushort capacity;
        public string[] tags;
        public string world;
    }
}