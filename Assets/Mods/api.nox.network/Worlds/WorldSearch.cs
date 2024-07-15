using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class WorldSearch : ShareObject
    {
        public World[] worlds;
        public uint total;
        public uint limit;
        public uint offset;
    }
}