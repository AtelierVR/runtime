using System.Collections.Generic;

namespace api.nox.network.Servers
{
    public class SearchRequest
    {
        internal string Server;
        internal string Query;
        internal uint Limit;
        internal uint Offset;

        public string ToParams()
        {
            var text = "";
            if (Query != null) text += (text.Length > 0 ? "&" : "") + $"query={Query}";
            if (Offset > 0) text += (text.Length > 0 ? "&" : "") + $"offset={Offset}";
            if (Limit > 0) text += (text.Length > 0 ? "&" : "") + $"limit={Limit}";
            return text;
        }

        public static SearchRequest From(Dictionary<string, object> data)
        {
            var req = new SearchRequest();
            if (data.TryGetValue("server", out var server) && server is string s) req.Server = s;
            if (data.TryGetValue("query", out var query) && query is string q) req.Query = q;
            if (data.TryGetValue("limit", out var limit) && limit is uint l) req.Limit = l;
            if (data.TryGetValue("offset", out var offset) && offset is uint o) req.Offset = o;
            return req;
        }
    }
}