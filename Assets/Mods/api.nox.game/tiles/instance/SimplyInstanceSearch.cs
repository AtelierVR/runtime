using Nox.CCK.Mods;

namespace api.nox.game
{
    public class SimplyInstanceSearch : ShareObject
    {
        public SimplyInstance[] instances;
        public uint total;
        public uint limit;
        public uint offset;
    }
}