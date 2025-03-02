using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace api.nox.network
{
    public class UpdateWorldData
    {
        public string Server;
        public uint WorldId;
        private string _title;
        private string _description;
        private ushort _capacity;

        internal string ToJson()
        {
            var obj = new JObject();
            if (!string.IsNullOrEmpty(_title)) obj["title"] = _title;
            if (!string.IsNullOrEmpty(_description)) obj["description"] = _description;
            if (_capacity > 0) obj["capacity"] = _capacity;
            return obj.ToString();
        }

        public static UpdateWorldData From(Dictionary<string, object> data)
        {
            var req = new UpdateWorldData();
            if (data.TryGetValue("server", out var server) && server is string s) req.Server = s;
            if (data.TryGetValue("worldId", out var worldId) && worldId is uint w) req.WorldId = w;
            if (data.TryGetValue("title", out var title) && title is string t) req._title = t;
            if (data.TryGetValue("description", out var description) && description is string d) req._description = d;
            if (data.TryGetValue("capacity", out var capacity) && capacity is ushort c) req._capacity = c;
            return req;
        }
    }
}