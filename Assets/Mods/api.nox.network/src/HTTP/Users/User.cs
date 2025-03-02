using System;
using api.nox.network.HTTP;
using Nox.CCK.Utils;

namespace api.nox.network.Users
{
    [Serializable]
    public class User : ICached, INoxObject
    {
        [NoxPublic(NoxAccess.Read)] public uint id;
        [NoxPublic(NoxAccess.Read)] public string username;
        [NoxPublic(NoxAccess.Read)] public string display;
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

        public string GetCacheKey() => GetStrictCacheKey();
        internal virtual string GetStrictCacheKey() => $"user.{id}.{server}";

        [NoxPublic(NoxAccess.Method)]
        public UserIdentifier ToIdentifier() => new(id.ToString(), server);

        public override string ToString() =>
            $"{GetType().Name}[id={id}, username={username}, display={display}, server={server}]";
    }
}