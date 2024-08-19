using System.Collections.Generic;

namespace api.nox.network
{
    public class WorldIdentifier
    {
        public uint id;
        public string server;
        public Dictionary<string, string> tags;

        // a string representation of the identifier, a list of tags separated by ";" and the server atfer a "@"
        public WorldIdentifier(uint id, string server, Dictionary<string, string> tags = null)
        {
            this.id = id;
            this.server = server;
            this.tags = tags ?? new Dictionary<string, string>();
        }

        public static WorldIdentifier FromString(string reference)
        {
            var parts = reference.Split('@');
            var server = parts.Length > 1 ? parts[1] : null;
            var content = parts[0].Split(';');
            var id = uint.Parse(content[0]);
            var tags = new Dictionary<string, string>();
            for (int i = 1; i < content.Length; i++)
            {
                var tag = content[i].Split('=');
                var key = tag[0];
                var value = string.Join('=', tag, 1, tag.Length - 1);
                tags[key] = value;
            }
            return new WorldIdentifier(id, server, tags);
        }

        public bool TryGetServer(out string server)
        {
            if (!string.IsNullOrEmpty(this.server))
            {
                server = this.server;
                return true;
            }
            server = null;
            return false;
        }

        public bool TryGetId(out uint id)
        {
            id = this.id;
            return true;
        }

        public bool IsLocal() => string.IsNullOrEmpty(server) || server == UserIdentifier.LocalServer;

        public string ToMinimalString(string defaultserver = null) => $"{id}@{(IsLocal() ? (defaultserver ?? UserIdentifier.LocalServer) : server)}";
    }
}