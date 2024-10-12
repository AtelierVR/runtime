using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyUserAPI : ShareObject
    {
        [ShareObjectExport, ShareObjectImport] public Func<UniTask<ShareObject>> SharedGetMyUser;
        [ShareObjectExport, ShareObjectImport] public Func<ShareObject, UniTask<ShareObject>> SharedSearchUsers;
        [ShareObjectExport, ShareObjectImport] public Func<ShareObject, UniTask<ShareObject>> SharedUpdateUser;
        [ShareObjectExport, ShareObjectImport] public Func<UniTask<bool>> SharedGetLogout;
        [ShareObjectExport, ShareObjectImport] public Func<string, string, string, UniTask<ShareObject>> SharedPostLogin;

        public async UniTask<SimplyUserMe> GetMyUser() => (await SharedGetMyUser())?.Convert<SimplyUserMe>();
        public async UniTask<SimplyUserSearch> SearchUsers(SimplySearchUserData data)
            => (await SharedSearchUsers(data))?.Convert<SimplyUserSearch>();
        public async UniTask<SimplyUserMe> UpdateUser(SimplyUserUpdate user) => (await SharedUpdateUser(user))?.Convert<SimplyUserMe>();
        public async UniTask<bool> GetLogout() => await SharedGetLogout();
        public async UniTask<SimplyUserLogin> PostLogin(string server, string username, string password)
            => (await SharedPostLogin(server, username, password))?.Convert<SimplyUserLogin>();
    }
}