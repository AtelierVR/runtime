using System;
using api.nox.network.Worlds;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace api.nox.network.Users
{
    [Serializable]
    public class UserMe : User
    {
        [NoxPublic(NoxAccess.Read)] public string email;
        [NoxPublic(NoxAccess.Read)] public uint created_at;
        [NoxPublic(NoxAccess.Read)] public string home;

        [NoxPublic(NoxAccess.Method)]
        public override bool MatchRef(string reference, string defaultServer)
        {
            var identifier = UserIdentifier.FromString(reference);
            if (new UserIdentifier(id.ToString(), server).ToMinimalString()
                == identifier.ToMinimalString(defaultServer)) return true;
            return new UserIdentifier(username, server).ToMinimalString()
                   == identifier.ToMinimalString(defaultServer);
        }

        internal override string GetStrictCacheKey() => $"user.{id}.{server}.me";

        [NoxPublic(NoxAccess.Method)]
        public async UniTask<World> GetHome()
        {
            if (string.IsNullOrEmpty(home)) return null;
            return await NetworkSystem.ModInstance.World.GetWorldByIdentifier(home, server);
        }
        
        [NoxPublic(NoxAccess.Method)]
        public override async UniTask<bool> Refresh()
        {
            var user = await NetworkSystem.ModInstance.User.GetUserById(server, id);;
            if (user == null) return false;
            user.CopyTo(this);
            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("user_fetch", this));
            NetworkSystem.CoreAPI.EventAPI.Emit(new NetEventContext("user_update", this));
            NetCache.Set(this);
            return true;
        }
        
        internal void CopyTo(UserMe user)
        {
            base.CopyTo(user);
            user.email = email;
            user.created_at = created_at;
            user.home = home;
        }
        
        
        public override string ToString() 
            => $"{GetType().Name}[id={id}, username={username}, display={display}, server={server}, email={email}, created_at={created_at}, home={home}]";
    }
}