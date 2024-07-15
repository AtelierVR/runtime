using Nox.CCK.Mods;

namespace api.nox.game
{
    public class SimplyServerSearch : ShareObject
    {
        public SimplyServer[] servers;
        public uint total;
        public uint limit;
        public uint offset;
    }
}