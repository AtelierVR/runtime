using System.Collections.Generic;
using Nox.CCK.Utils;

namespace api.nox.network
{
    public class WorldIdentifier
    {
        [NoxPublic(NoxAccess.Field)] public uint ID;
        [NoxPublic(NoxAccess.Field)] public string Server;
        [NoxPublic(NoxAccess.Field)] public Dictionary<string, string> Tags;

        public WorldIdentifier(uint id, string server, Dictionary<string, string> tags = null)
        {
            ID = id;
            Server = server;
            Tags = tags ?? new Dictionary<string, string>();
        }

        public static WorldIdentifier FromString(string reference)
        {
            var parts = reference.Split('@');
            var server = parts.Length > 1 ? parts[1] : null;
            var content = parts[0].Split(';');
            var id = uint.Parse(content[0]);
            var tags = new Dictionary<string, string>();
            for (var i = 1; i < content.Length; i++)
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
            if (!string.IsNullOrEmpty(this.Server))
            {
                server = this.Server;
                return true;
            }

            server = null;
            return false;
        }

        public bool TryGetId(out uint id)
        {
            id = this.ID;
            return true;
        }

        [NoxPublic(NoxAccess.Method)]
        public bool IsLocal() => string.IsNullOrEmpty(Server) || Server == UserIdentifier.LocalServer;

        [NoxPublic(NoxAccess.Method)]
        public string ToMinimalString(string defaultServer = null) 
            => $"{ID}@{(IsLocal() ? defaultServer ?? UserIdentifier.LocalServer : Server)}";

        [NoxPublic(NoxAccess.Method)]
        public string ToFullString(string defaultServer = null) 
            => $"{ID}{(Tags.Count > 0 ? ";" : "")}{string.Join(';', Tags)}@{(IsLocal() ? defaultServer ?? UserIdentifier.LocalServer : Server)}";
    }
}