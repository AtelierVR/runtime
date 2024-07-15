using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace api.nox.game
{
    public class SimplyUserAPI : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public Func<UniTask<ShareObject>> SharedGetMyUser;
        [ShareObjectImport, ShareObjectExport] public Func<string, string, uint, uint, UniTask<ShareObject>> SharedSearchUsers;

        public async UniTask<SimplyUserMe> GetMyUser() => (await SharedGetMyUser()).Convert<SimplyUserMe>();
        public async UniTask<SimplyUserSearch> SearchUsers(string server, string query, uint offset = 0, uint limit = 10)
            => (await SharedSearchUsers(server, query, offset, limit)).Convert<SimplyUserSearch>();
    }
}