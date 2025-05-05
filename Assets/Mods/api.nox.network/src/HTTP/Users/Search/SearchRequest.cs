using System.Collections.Generic;

namespace api.nox.network.Users
{
    public class SearchRequest
    {
        public string Server;
        public string Query;
        public string[] UserIds;
        public uint Offset;
        public uint Limit;

        public string ToParams()
        {
            var text = "";
            if (!string.IsNullOrEmpty(Query))
                text += (text.Length > 0 ? "&" : "") + $"query={Query}";
            if (UserIds != null)
                foreach (var u in UserIds)
                    text += (text.Length > 0 ? "&" : "") + $"id={u}";
            if (Offset > 0) text += (text.Length > 0 ? "&" : "") + $"offset={Offset}";
            if (Limit > 0) text += (text.Length > 0 ? "&" : "") + $"limit={Limit}";
            return text;
        }

        public static SearchRequest From(Dictionary<string, object> data)
        {
            var req = new SearchRequest();
            if (data.TryGetValue("server", out var server) && server is string s) req.Server = s;
            if (data.TryGetValue("query", out var query) && query is string q) req.Query = q;
            if (data.TryGetValue("user_ids", out var userIds) && userIds is string[] u) req.UserIds = u;
            if (data.TryGetValue("offset", out var offset) && offset is uint o) req.Offset = o;
            if (data.TryGetValue("limit", out var limit) && limit is uint l) req.Limit = l;
            return req;
        }
    }
}