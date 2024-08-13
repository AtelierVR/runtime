using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyUserAPI : ShareObject
    {
        [ShareObjectImport] public Func<UniTask<ShareObject>> SharedGetMyUser;
        [ShareObjectImport] public Func<string, string, uint, uint, UniTask<ShareObject>> SharedSearchUsers;
        [ShareObjectImport] public Func<ShareObject, UniTask<ShareObject>> SharedUpdateUser;
        [ShareObjectImport] public Func<UniTask<bool>> SharedGetLogout;
        [ShareObjectImport] public Func<string, string, string, UniTask<ShareObject>> SharedPostLogin;

        public async UniTask<SimplyUserMe> GetMyUser() => (await SharedGetMyUser())?.Convert<SimplyUserMe>();
        public async UniTask<SimplyUserSearch> SearchUsers(string server, string query, uint offset = 0, uint limit = 10)
            => (await SharedSearchUsers(server, query, offset, limit)).Convert<SimplyUserSearch>();
            
        public async UniTask<SimplyUserMe> UpdateUser(SimplyUserUpdate user) => (await SharedUpdateUser(user))?.Convert<SimplyUserMe>();
        public async UniTask<bool> GetLogout() => await SharedGetLogout();
        public async UniTask<SimplyUserLogin> PostLogin(string server, string username, string password)
            => (await SharedPostLogin(server, username, password))?.Convert<SimplyUserLogin>();
    }
}