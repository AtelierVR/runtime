using Nox.CCK.Mods;
using Nox.CCK.Servers;

namespace api.nox.network
{
    [System.Serializable]
    public class ServerSearch : ShareObject
    {
        public Server[] servers;
        [ShareObjectExport] public uint total;
        [ShareObjectExport] public uint limit;
        [ShareObjectExport] public uint offset;
        
        [ShareObjectExport] public ShareObject[] SharedServers;

        public void BeforeExport()
        {
            SharedServers = new ShareObject[servers.Length];
            for (int i = 0; i < servers.Length; i++)
                SharedServers[i] = servers[i];
        }

        public void AfterExport()
        {
            SharedServers = null;
        }
    }
}