using System.Collections.Generic;
using System.Linq;

namespace api.nox.network.Worlds
{
    public class SearchWorldData
    {
        internal string Server;
        internal string Query;
        internal uint[] WorldIds;
        internal uint Offset;
        internal uint Limit;

        public string ToParams()
        {
            var text = "";
            if (Query != null) text += (text.Length > 0 ? "&" : "") + $"query={Query}";
            if (WorldIds != null)
                text = WorldIds.Aggregate(text, (current, u)
                    => current + ((current.Length > 0 ? "&" : "") + $"id={u}"));
            if (Offset > 0) text += (text.Length > 0 ? "&" : "") + $"offset={Offset}";
            if (Limit > 0) text += (text.Length > 0 ? "&" : "") + $"limit={Limit}";
            return text;
        }

        public static SearchWorldData From(Dictionary<string, object> data)
        {
            var req = new SearchWorldData();
            if (data.TryGetValue("server", out var server) && server is string s) req.Server = s;
            if (data.TryGetValue("query", out var query) && query is string q) req.Query = q;
            if (data.TryGetValue("world_ids", out var worldIds) && worldIds is uint[] u) req.WorldIds = u;
            if (data.TryGetValue("offset", out var offset) && offset is uint o) req.Offset = o;
            if (data.TryGetValue("limit", out var limit) && limit is uint l) req.Limit = l;
            return req;
        }
    }
}