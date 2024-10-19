namespace api.nox.network.Worlds
{
    [System.Serializable]
    public class WorldSearch
    {
        public World[] worlds;
        public uint total;
        public uint limit;
        public uint offset;

        public override string ToString() => $"{GetType().Name}[total={total}, limit={limit}, offset={offset}, worlds={worlds?.Length}]";
    }
}