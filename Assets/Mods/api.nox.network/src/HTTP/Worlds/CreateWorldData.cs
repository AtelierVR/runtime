using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace api.nox.network
{
    public class CreateWorldData
    {
        public string Server;
        private uint _id;
        private string _title;
        private string _description;
        private ushort _capacity;
        private string _thumbnail;
        private bool _customID;

        internal string ToJson()
        {
            var obj = new JObject();
            if (_customID) obj["id"] = _id;
            if (!string.IsNullOrEmpty(_title)) obj["title"] = _title;
            if (!string.IsNullOrEmpty(_description)) obj["description"] = _description;
            if (_capacity > 0) obj["capacity"] = _capacity;
            if (!string.IsNullOrEmpty(_thumbnail)) obj["thumbnail"] = _thumbnail;
            return obj.ToString();
        }

        public static CreateWorldData From(Dictionary<string, object> data)
        {
            var req = new CreateWorldData();
            if (data.TryGetValue("server", out var server) && server is string s) req.Server = s;
            if (data.TryGetValue("id", out var id) && id is uint i) req._id = i;
            if (data.TryGetValue("title", out var title) && title is string t) req._title = t;
            if (data.TryGetValue("description", out var description) && description is string d) req._description = d;
            if (data.TryGetValue("capacity", out var capacity) && capacity is ushort c) req._capacity = c;
            if (data.TryGetValue("thumbnail", out var thumbnail) && thumbnail is string th) req._thumbnail = th;
            if (data.TryGetValue("custom_id", out var customID) && customID is bool ci) req._customID = ci;
            return req;
        }
    }
}