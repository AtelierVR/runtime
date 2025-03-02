using System.Collections.Generic;
using System.Linq;

namespace api.nox.network.Worlds.Assets
{
    public class SearchRequest
    {
        public string Server;
        public uint WorldID;
        public uint Offset;
        public uint Limit;
        public string[] Platforms;
        public string[] Engines;
        public ushort[] Versions;
        public bool WithEmpty;

        public string ToParams()
        {
            var text = "";
            if (Offset > 0) text += (text.Length > 0 ? "&" : "") + $"offset={Offset}";
            if (Limit > 0) text += (text.Length > 0 ? "&" : "") + $"limit={Limit}";
            if (Platforms != null) text = Platforms.Aggregate(text, (current, platform) 
                => current + (current.Length > 0 ? "&" : "") + $"platform={platform}");
            if (Engines != null) text = Engines.Aggregate(text, (current, engine) 
                => current + (current.Length > 0 ? "&" : "") + $"engine={engine}");
            if (Versions != null) text = Versions.Aggregate(text, (current, version) 
                => current + (current.Length > 0 ? "&" : "") + $"version={version}");
            if (WithEmpty) text += "&empty=true";
            return text;
        }

        public static SearchRequest From(Dictionary<string, object> data)
        {
            var req = new SearchRequest();
            if (data.TryGetValue("server", out var server) && server is string s) req.Server = s;
            if (data.TryGetValue("world_id", out var worldId) && worldId is uint w) req.WorldID = w;
            if (data.TryGetValue("offset", out var offset) && offset is uint o) req.Offset = o;
            if (data.TryGetValue("limit", out var limit) && limit is uint l) req.Limit = l;
            if (data.TryGetValue("platforms", out var platforms) && platforms is string[] p) req.Platforms = p;
            if (data.TryGetValue("engines", out var engines) && engines is string[] e) req.Engines = e;
            if (data.TryGetValue("versions", out var versions) && versions is ushort[] v) req.Versions = v;
            if (data.TryGetValue("with_empty", out var withEmpty) && withEmpty is bool we) req.WithEmpty = we;
            return req;
        }
    }
}