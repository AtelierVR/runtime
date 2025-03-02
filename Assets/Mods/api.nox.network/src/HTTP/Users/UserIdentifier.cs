using Nox.CCK.Utils;

namespace api.nox.network
{
    public class UserIdentifier
    {
        public const string LocalServer = "::";

        public UserIdentifier(string identifier, string server = null)
        {
            Identifier = identifier;
            Server = server;
        }

        public static UserIdentifier FromString(string reference)
        {
            var parts = reference.Split('@');
            return new UserIdentifier(parts[0], parts.Length > 1 ? parts[1] : null);
        }

        [NoxPublic(NoxAccess.Field)] public string Identifier;
        [NoxPublic(NoxAccess.Field)] public string Server;

        public bool TryGetUsername(out string username)
        {
            if (!string.IsNullOrEmpty(Identifier))
            {
                username = Identifier;
                return true;
            }

            username = null;
            return false;
        }

        [NoxPublic(NoxAccess.Method)]
        public bool IsId() => TryGetId(out _);

        public bool TryGetId(out uint id)
        {
            if (uint.TryParse(Identifier, out id))
                return true;
            id = 0;
            return false;
        }

        [NoxPublic(NoxAccess.Method)]
        public bool IsLocal() => string.IsNullOrEmpty(Server) || Server == LocalServer;

        [NoxPublic(NoxAccess.Method)]
        public string ToMinimalString(string defaultServer = null) 
            => $"{Identifier}@{(IsLocal() ? defaultServer ?? LocalServer : Server)}";
    }
}