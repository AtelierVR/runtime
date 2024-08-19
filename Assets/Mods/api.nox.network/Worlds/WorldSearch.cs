using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class WorldSearch : ShareObject
    {
        public World[] worlds;
        [ShareObjectExport] public uint total;
        [ShareObjectExport] public uint limit;
        [ShareObjectExport] public uint offset;

        [ShareObjectExport] public ShareObject[] SharedWorlds;

        public void BeforeExport()
        {
            SharedWorlds = new ShareObject[worlds.Length];
            for (int i = 0; i < worlds.Length; i++)
                SharedWorlds[i] = worlds[i];
        }

        public void AfterExport()
        {
            SharedWorlds = null;
        }

        public override string ToString() => $"{GetType().Name}[total={total}, limit={limit}, offset={offset}, worlds={worlds?.Length}]";
    }
}