using System.Collections.Generic;
using Nox.CCK.Utils;

namespace api.nox.network.Instances
{
    public class SearchRequest : INoxObject
    {
        public string Server;
        public string Query;
        public string World;
        public string Owner;
        public uint Offset;
        public uint Limit;

        public static SearchRequest From(Dictionary<string, object> d)
        {
            var s = new SearchRequest();
            foreach (var key in d.Keys)
                switch (key)
                {
                    case "server" when d[key] is string server:
                        s.Server = server;
                        break;
                    case "query" when d[key] is string query:
                        s.Query = query;
                        break;
                    case "world" when d[key] is string world:
                        s.World = world;
                        break;
                    case "owner" when d[key] is string owner:
                        s.Owner = owner;
                        break;
                    case "offset" when d[key] is uint offset:
                        s.Offset = offset;
                        break;
                    case "limit" when d[key] is uint limit:
                        s.Limit = limit;
                        break;
                }
            return s;
        }

        public string ToParams()
        {
            var text = "";
            if (Query != null) text += (text.Length > 0 ? "&" : "") + $"query={Query}";
            if (World != null) text += (text.Length > 0 ? "&" : "") + $"world={World}";
            if (Owner != null) text += (text.Length > 0 ? "&" : "") + $"owner={Owner}";
            if (Offset > 0) text += (text.Length > 0 ? "&" : "") + $"offset={Offset}";
            if (Limit > 0) text += (text.Length > 0 ? "&" : "") + $"limit={Limit}";
            return text;
        }
    }
}