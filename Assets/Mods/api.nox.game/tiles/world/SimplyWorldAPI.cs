using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace api.nox.game
{
    public class SimplyWorldAPI : ShareObject
    {
        [ShareObjectImport] public Func<string, string, uint, uint, UniTask<ShareObject>> SharedSearchWorlds;
        public async UniTask<SimplyWorldSearch> SearchWorlds(string server, string query, uint offset = 0, uint limit = 10)
            => (await SharedSearchWorlds(server, query, offset, limit)).Convert<SimplyWorldSearch>();
        [ShareObjectImport] public Func<string, uint[], UniTask<ShareObject[]>> SharedGetWorlds;
        public async UniTask<SimplyWorld[]> GetWorlds(string server, params uint[] ids)
        {
            var worlds = await SharedGetWorlds(server, ids);
            var result = new SimplyWorld[worlds.Length];
            for (var i = 0; i < worlds.Length; i++)
                result[i] = worlds[i].Convert<SimplyWorld>();
            return result;
        }
    }
}