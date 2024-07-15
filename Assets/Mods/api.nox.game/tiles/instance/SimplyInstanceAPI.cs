using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace api.nox.game
{
    public class SimplyInstanceAPI : ShareObject
    {
        [ShareObjectImport] public Func<string, string, uint, uint, UniTask<ShareObject>> SharedSearchInstances;
        public async UniTask<SimplyInstanceSearch> SearchInstances(string server, string query, uint offset = 0, uint limit = 10)
            => (await SharedSearchInstances(server, query, offset, limit)).Convert<SimplyInstanceSearch>();
    }
}