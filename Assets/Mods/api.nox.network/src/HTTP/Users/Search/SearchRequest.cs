using System.Collections.Generic;

namespace api.nox.network.Users
{
    public class SearchRequest
    {
        public string server;
        public string query;
        public string[] user_ids;
        public uint offset;
        public uint limit;

        public string ToParams()
        {
            var text = "";
            if (query != null) text += (text.Length > 0 ? "&" : "") + $"query={query}";
            if (user_ids != null)
                foreach (var u in user_ids)
                    text += (text.Length > 0 ? "&" : "") + $"id={u}";
            if (offset > 0) text += (text.Length > 0 ? "&" : "") + $"offset={offset}";
            if (limit > 0) text += (text.Length > 0 ? "&" : "") + $"limit={limit}";
            return text;
        }

        public static SearchRequest From(Dictionary<string, object> data)
        {
            var req = new SearchRequest();
            if (data.TryGetValue("server", out var server) && server is string s) req.server = s;
            if (data.TryGetValue("query", out var query) && query is string q) req.query = q;
            if (data.TryGetValue("user_ids", out var userIds) && userIds is string[] u) req.user_ids = u;
            if (data.TryGetValue("offset", out var offset) && offset is uint o) req.offset = o;
            if (data.TryGetValue("limit", out var limit) && limit is uint l) req.limit = l;
            return req;
        }
    }
}