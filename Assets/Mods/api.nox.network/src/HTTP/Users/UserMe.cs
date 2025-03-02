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
            var worldref = WorldIdentifier.FromString(home);
            return await NetworkSystem.ModInstance.World.GetWorld(worldref.Server ?? server, worldref.ID);
        }
    }
}