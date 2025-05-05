using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace api.nox.network.Users
{
    [Serializable]
    public class UserResponse : INoxObject
    {
        internal string query;
        internal string[] id;

        [NoxPublic(NoxAccess.Read)] public User[] users;
        [NoxPublic(NoxAccess.Read)] public uint total;
        [NoxPublic(NoxAccess.Read)] public uint limit;
        [NoxPublic(NoxAccess.Read)] public uint offset;

        [NoxPublic(NoxAccess.Method)]
        public bool HasNext() => offset + limit < total;

        [NoxPublic(NoxAccess.Method)]
        public bool HasPrevious() => offset > 0;

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<UserResponse> Next()
            => HasNext() && NetworkSystem.ModInstance.User != null
                ? await NetworkSystem.ModInstance.User.SearchUsers(new SearchRequest()
                {
                    Query = query,
                    UserIds = id,
                    Offset = offset + limit,
                    Limit = limit
                })
                : null;

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<UserResponse> Previous()
            => HasPrevious() && NetworkSystem.ModInstance.User != null
                ? await NetworkSystem.ModInstance.User.SearchUsers(new SearchRequest()
                {
                    Query = query,
                    UserIds = id,
                    Offset = offset - limit,
                    Limit = limit
                })
                : null;
    }
}