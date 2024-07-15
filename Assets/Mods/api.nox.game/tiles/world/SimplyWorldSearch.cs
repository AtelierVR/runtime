using Nox.CCK.Mods;

namespace api.nox.game
{
    public class SimplyWorldSearch : ShareObject
    {
        public SimplyWorld[] worlds;
        public uint total;
        public uint limit;
        public uint offset;
    }
}