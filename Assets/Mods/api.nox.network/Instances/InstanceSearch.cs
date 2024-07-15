using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class InstanceSearch : ShareObject
    {
        public Instance[] instances;
        public uint total;
        public uint limit;
        public uint offset;
    }
}