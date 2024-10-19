using System;
using Cysharp.Threading.Tasks;

namespace api.nox.network.Users
{
    [Serializable]
    public class UserResponse
    {
        internal string query;
        internal string[] id;
        public User[] users;
        public uint total;
        public uint limit;
        public uint offset;

        public bool HasNext() => offset + limit < total;
        public bool HasPrevious() => offset > 0;

        public async UniTask<UserResponse> Next()
            => HasNext() && NetworkSystem.ModInstance.User != null ? await NetworkSystem.ModInstance.User.SearchUsers(new()
            {
                query = query,
                user_ids = id,
                offset = offset + limit,
                limit = limit
            }) : null;

        public async UniTask<UserResponse> Previous()
            => HasPrevious() && NetworkSystem.ModInstance.User != null ? await NetworkSystem.ModInstance.User.SearchUsers(new()
            {
                query = query,
                user_ids = id,
                offset = offset - limit,
                limit = limit
            }) : null;
    }
}