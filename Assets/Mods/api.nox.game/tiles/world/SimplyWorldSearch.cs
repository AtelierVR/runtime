using Nox.CCK.Mods;

namespace api.nox.game
{
    public class SimplyWorldSearch : ShareObject
    {
        public SimplyWorld[] worlds;
        [ShareObjectImport] public uint total;
        [ShareObjectImport] public uint limit;
        [ShareObjectImport] public uint offset;

        [ShareObjectImport] public ShareObject[] SharedWorlds;

        public void BeforeImport()
        {
            worlds = null;
        }

        public void AfterImport()
        {
            worlds = new SimplyWorld[SharedWorlds.Length];
            for (int i = 0; i < SharedWorlds.Length; i++)
                worlds[i] = SharedWorlds[i].Convert<SimplyWorld>();
        }
    }
}