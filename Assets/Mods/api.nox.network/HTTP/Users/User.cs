using System;
using api.nox.network.HTTP;

namespace api.nox.network.Users
{
    [Serializable]
    public class User : ICached
    {
        public uint id;
        public string username;
        public string display;
        public string[] tags;
        public string server;
        public float rank;
        public string[] links;
        public string banner;
        public string thumbnail;

        public virtual bool MatchRef(string reference, string default_server)
        {
            var identifier = UserIdentifier.FromString(reference);
            if (new UserIdentifier(id.ToString(), server).ToMinimalString() == identifier.ToMinimalString(default_server)) return true;
            if (new UserIdentifier(username, server).ToMinimalString() == identifier.ToMinimalString(default_server)) return true;
            return false;
        }

        public string GetCacheKey() => $"user.{id}.{server}";
        public UserIdentifier ToIdentifier() => new(id.ToString(), server);
    }
}