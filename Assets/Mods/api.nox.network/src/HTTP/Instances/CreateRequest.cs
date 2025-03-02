using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace api.nox.network.Instances
{
    public class CreateRequest
    {
        public string Server;

        private string _name;
        private string _title;
        private string _description;
        private string _expose;
        private string _world;
        private ushort _capacity;
        private bool _usePassword;
        private string _password;
        private bool _useWhitelist;
        private string[] _whitelist;
        private string _thumbnail;

        internal string ToJson()
        {
            var obj = new JObject();
            if (!string.IsNullOrEmpty(_name)) obj["name"] = _name;
            if (!string.IsNullOrEmpty(_title)) obj["title"] = _title;
            if (!string.IsNullOrEmpty(_description)) obj["description"] = _description;
            if (!string.IsNullOrEmpty(_thumbnail)) obj["thumbnail"] = _thumbnail;
            obj["expose"] = _expose;
            obj["world"] = _world;
            obj["capacity"] = _capacity;
            obj["use_password"] = _usePassword;
            if (string.IsNullOrEmpty(_password)) obj["password"] = _password;
            obj["use_whitelist"] = _useWhitelist;
            if (_whitelist != null) obj["whitelist"] = JArray.FromObject(_whitelist);
            return obj.ToString();
        }

        public static CreateRequest From(Dictionary<string, object> data)
        {
            var req = new CreateRequest();
            if (data.TryGetValue("server", out var server) && server is string s) req.Server = s;
            if (data.TryGetValue("name", out var name) && name is string n) req._name = n;
            if (data.TryGetValue("title", out var title) && title is string t) req._title = t;
            if (data.TryGetValue("description", out var description) && description is string d) req._description = d;
            if (data.TryGetValue("thumbnail", out var thumbnail) && thumbnail is string th) req._thumbnail = th;
            if (data.TryGetValue("expose", out var expose) && expose is string e) req._expose = e;
            if (data.TryGetValue("world", out var world) && world is string w) req._world = w;
            if (data.TryGetValue("capacity", out var capacity) && capacity is ushort c) req._capacity = c;
            if (data.TryGetValue("use_password", out var usePassword) && usePassword is bool up) req._usePassword = up;
            if (data.TryGetValue("password", out var password) && password is string p) req._password = p;
            if (data.TryGetValue("use_whitelist", out var uwo) && uwo is bool uw) req._useWhitelist = uw;
            if (data.TryGetValue("whitelist", out var whitelist) && whitelist is string[] wl) req._whitelist = wl;
            return req;
        }
    }
}