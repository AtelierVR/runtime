using System;
using api.nox.network.HTTP;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace api.nox.network.Users
{
    [Serializable]
    public class User : ICached, INoxObject
    {
        [NoxPublic(NoxAccess.Read)] public uint id;
        [NoxPublic(NoxAccess.Read)] public string username;
        [NoxPublic(NoxAccess.Read)] public string display;
        [NoxPublic(NoxAccess.Read)] public string bio;
        [NoxPublic(NoxAccess.Read)] public string[] tags;
        [NoxPublic(NoxAccess.Read)] public string server;
        [NoxPublic(NoxAccess.Read)] public float rank;
        [NoxPublic(NoxAccess.Read)] public string[] links;
        [NoxPublic(NoxAccess.Read)] public string banner;
        [NoxPublic(NoxAccess.Read)] public string thumbnail;

        public virtual bool MatchRef(string reference, string defaultServer)
        {
            var identifier = UserIdentifier.FromString(reference);
            if (new UserIdentifier(id.ToString(), server).ToMinimalString() ==
                identifier.ToMinimalString(defaultServer)) return true;
            if (new UserIdentifier(username, server).ToMinimalString() ==
                identifier.ToMinimalString(defaultServer)) return true;
            return false;
        }

        [NoxPublic(NoxAccess.Method)]
        public virtual bool IsCurrent()
        {
            var user = NetworkSystem.ModInstance.User.CurrentUser;
            return user != null
                   && (user.username == username || user.id == id)
                   && user.server == server;
        }

        [NoxPublic(NoxAccess.Method)]
        public virtual async UniTask<bool> Refresh()
        {
            var user = await NetworkSystem.ModInstance.User.GetUserById(server, id);;
            if (user == null) return false;
            user.CopyTo(this);
            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("user_fetch", this));
            NetCache.Set(this);
            return true;
        }
        
        internal void CopyTo(User user)
        {
            user.id = id;
            user.username = username;
            user.display = display;
            user.bio = bio;
            user.tags = tags;
            user.server = server;
            user.rank = rank;
            user.links = links;
            user.banner = banner;
            user.thumbnail = thumbnail;
        }
        
        public string GetCacheKey() => GetStrictCacheKey();
        internal virtual string GetStrictCacheKey() => $"user.{id}.{server}";

        [NoxPublic(NoxAccess.Method)]
        public UserIdentifier ToIdentifier(bool useUsername = false)
            => new(useUsername ? username : id.ToString(), server);

        public override string ToString()
            => $"{GetType().Name}[id={id}, username={username}, display={display}, server={server}]";
    }
}