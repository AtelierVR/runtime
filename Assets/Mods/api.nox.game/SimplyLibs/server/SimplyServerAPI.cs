using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyServerAPI : ShareObject
    {
        [ShareObjectImport] public Func<UniTask<ShareObject>> SharedGetMyServer;
        [ShareObjectImport] public Func<string, string, uint, uint, UniTask<ShareObject>> SharedSearchServers;

        public async UniTask<SimplyServer> GetMyServer() => (await SharedGetMyServer()).Convert<SimplyServer>();
        public async UniTask<SimplyServerSearch> SearchServers(string server, string query, uint offset = 0, uint limit = 10)
            => (await SharedSearchServers(server, query, offset, limit)).Convert<SimplyServerSearch>();
    }
}