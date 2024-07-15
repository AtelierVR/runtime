using Nox.CCK.Mods;

namespace api.nox.game
{
    public class SimplyServerSearch : ShareObject
    {
        public SimplyServer[] servers;
        [ShareObjectImport] public uint total;
        [ShareObjectImport] public uint limit;
        [ShareObjectImport] public uint offset;

        [ShareObjectImport] public ShareObject[] SharedServers;

        public void BeforeImport()
        {
            servers = null;
        }

        public void AfterImport()
        {
            servers = new SimplyServer[SharedServers.Length];
            for (int i = 0; i < SharedServers.Length; i++)
                servers[i] = SharedServers[i].Convert<SimplyServer>();
        }
    }
}