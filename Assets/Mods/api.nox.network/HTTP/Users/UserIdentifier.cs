namespace api.nox.network
{
    public class UserIdentifier
    {
        public const string LocalServer = "::";

        public UserIdentifier(string identifier, string server = null)
        {
            this.identifier = identifier;
            this.server = server;
        }

        public static UserIdentifier FromString(string reference)
        {
            var parts = reference.Split('@');
            return new UserIdentifier(parts[0], parts.Length > 1 ? parts[1] : null);
        }

        public string identifier;
        public string server;

        public bool TryGetUsername(out string username)
        {
            if (!string.IsNullOrEmpty(identifier))
            {
                username = identifier;
                return true;
            }
            username = null;
            return false;
        }

        public bool IsId() => TryGetId(out _);

        public bool TryGetId(out uint id)
        {
            if (uint.TryParse(identifier, out id))
                return true;
            id = 0;
            return false;
        }

        public bool IsLocal() => string.IsNullOrEmpty(server) || server == LocalServer;

        public string ToMinimalString(string defaultserver = null) => $"{identifier}@{(IsLocal() ? (defaultserver ?? LocalServer) : server)}";
    }
}