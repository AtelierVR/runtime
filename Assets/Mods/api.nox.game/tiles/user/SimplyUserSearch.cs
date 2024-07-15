using Nox.CCK.Mods;

namespace api.nox.game
{
    public class SimplyUserSearch : ShareObject
    {
        public SimplyUser[] users;
        public uint total;
        public uint limit;
        public uint offset;
    }
}