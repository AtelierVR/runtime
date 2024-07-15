using Nox.CCK.Mods;
using Nox.CCK.Servers;

namespace api.nox.network
{
    [System.Serializable]
    public class ServerSearch : ShareObject
    {
        public Server[] servers;
        public uint total;
        public uint limit;
        public uint offset;
    }
}