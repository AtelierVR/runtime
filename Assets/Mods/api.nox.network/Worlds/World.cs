using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class World : ShareObject
    {
        public uint id;
        public string title;
        public string description;
        public ushort capacity;
        public string[] tags;
        public string owner;
        public string server;
        public string thumbnail;
        public WorldAsset[] assets;
    }
}