namespace api.nox.network
{
    /**
     * @brief A class that represents a user identifier.
     */
    public class UserIdentifier
    {
        /**
         * @brief Default server address.
         */
        public const string LocalServer = "::";

        public UserIdentifier(string identifier, string server = null)
        {
            this.identifier = identifier;
            this.server = server;
        }

        /**
         * @brief Create a UserIdentifier from a string reference (<id>[@<server>]).
         * @param reference The string to parse.
         * @return The UserIdentifier.
         */
        public static UserIdentifier FromString(string reference)
        {
            var parts = reference.Split('@');
            return new UserIdentifier(parts[0], parts.Length > 1 ? parts[1] : null);
        }

        /**
         * @brief The user identifier (username or id).
         */
        public string identifier;

        /**
         * @brief The server address.
         */
        public string server;

        /**
         * @brief Try to get the username.
         * @param username The username.
         * @return True if the username is valid.
         */
        public bool TryByUsername(out string username)
        {
            if (!string.IsNullOrEmpty(identifier))
            {
                username = identifier;
                return true;
            }
            username = null;
            return false;
        }

        /**
         * @brief Try to get the id.
         * @param id The id.
         * @return True if the id is valid.
         */
        public bool TryById(out uint id)
        {
            if (uint.TryParse(identifier, out id))
                return true;
            id = 0;
            return false;
        }

        /**
         * @brief Check if the reference is local.
         * @return True if the reference is local.
         */
        public bool IsLocal() => string.IsNullOrEmpty(server) || server == LocalServer;

        /**
         * @brief Get string reference.
         * @param defaultserver The default server address if the reference is local.
         * @return The string reference.
         */
        public string ToMinimalString(string defaultserver = null) => $"{identifier}@{(IsLocal() ? (defaultserver ?? LocalServer) : server)}";
    }
}