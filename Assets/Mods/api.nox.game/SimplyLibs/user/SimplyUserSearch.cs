using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyUserSearch : ShareObject
    {
        public SimplyUser[] users;
        [ShareObjectImport] public uint total;
        [ShareObjectImport] public uint limit;
        [ShareObjectImport] public uint offset;
        
        [ShareObjectImport, ShareObjectExport] public Func<bool> SharedHasNext;
        public bool HasNext() => SharedHasNext();
        [ShareObjectImport, ShareObjectExport] public Func<bool> SharedHasPrevious;
        public bool HasPrevious() => SharedHasPrevious();
        [ShareObjectImport, ShareObjectExport] public Func<UniTask<ShareObject>> SharedNext;
        public async UniTask<SimplyUserSearch> Next() => (await SharedNext()).Convert<SimplyUserSearch>();
        [ShareObjectImport, ShareObjectExport] public Func<UniTask<ShareObject>> SharedPrevious;
        public async UniTask<SimplyUserSearch> Previous() => (await SharedPrevious()).Convert<SimplyUserSearch>();


        
        [ShareObjectImport] public ShareObject[] SharedUsers;

        public void BeforeImport()
        {
            users = null;
        }

        public void AfterImport()
        {
            users = new SimplyUser[SharedUsers.Length];
            for (int i = 0; i < SharedUsers.Length; i++)
                users[i] = SharedUsers[i].Convert<SimplyUser>();
        }
    }
}