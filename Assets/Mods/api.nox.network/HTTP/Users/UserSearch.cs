using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace api.nox.network
{

    [System.Serializable]
    public class UserSearch : ShareObject
    {
        internal NetworkSystem netSystem;
        internal string query;
        internal string[] id;
        public User[] users;
        [ShareObjectExport] public uint total;
        [ShareObjectExport] public uint limit;
        [ShareObjectExport] public uint offset;

        public bool HasNext() => offset + limit < total;
        public bool HasPrevious() => offset > 0;

        public async UniTask<UserSearch> Next()
            => HasNext() ? await netSystem.User.SearchUsers(new()
            {
                query = query,
                user_ids = id,
                offset = offset + limit,
                limit = limit
            }) : null;

        public async UniTask<UserSearch> Previous()
            => HasPrevious() ? await netSystem.User.SearchUsers(new()
            {
                query = query,
                user_ids = id,
                offset = offset - limit,
                limit = limit
            }) : null;

        [ShareObjectExport] public ShareObject[] SharedUsers;


        [ShareObjectExport] public ShareObject[] SharedInstances;
        [ShareObjectExport] public Func<bool> SharedHasNext;
        [ShareObjectExport] public Func<bool> SharedHasPrevious;
        [ShareObjectExport] public Func<UniTask<ShareObject>> SharedNext;
        [ShareObjectExport] public Func<UniTask<ShareObject>> SharedPrevious;

        public void BeforeExport()
        {
            SharedUsers = new ShareObject[users.Length];
            for (int i = 0; i < users.Length; i++)
                SharedUsers[i] = users[i];
            SharedHasNext = HasNext;
            SharedHasPrevious = HasPrevious;
            SharedNext = async () => await Next();
            SharedPrevious = async () => await Previous();
        }

        public void AfterExport()
        {
            SharedUsers = null;
            SharedHasNext = null;
            SharedHasPrevious = null;
            SharedNext = null;
            SharedPrevious = null;
        }
    }
}