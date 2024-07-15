using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class WorldAsset : ShareObject
    {
        public uint id;
        public uint version;
        public string engine;
        public string platform;
        public bool is_empty;
        public string url;
        public string hash;
        public uint size;
    }
}