using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.network
{
    public class CreateAssetData
    {
        public string Server;
        public uint WorldId;
        private uint _id;
        public ushort Version;
        public string Engine;
        public string Platform;
        private string _url;
        private string _hash;
        private uint _size;

        internal string ToJson()
        {
            var obj = new JObject();
            if (_id > 0) obj["id"] = _id;
            obj["version"] = Version;
            obj["engine"] = Engine;
            obj["platform"] = Platform;
            if (!string.IsNullOrEmpty(_url)) obj["url"] = _url;
            if (!string.IsNullOrEmpty(_hash)) obj["hash"] = _hash;
            if (_size > 0) obj["size"] = _size;
            return obj.ToString();
        }

        public static CreateAssetData From(Dictionary<string, object> data)
        {
            var req = new CreateAssetData();
            if (data.TryGetValue("server", out var server) && server is string s) req.Server = s;
            if (data.TryGetValue("world_id", out var worldId) && worldId is uint w) req.WorldId = w;
            if (data.TryGetValue("id", out var id) && id is uint i) req._id = i;
            if (data.TryGetValue("version", out var version) && version is ushort v) req.Version = v;
            if (data.TryGetValue("engine", out var engine) && engine is string e) req.Engine = e;
            if (data.TryGetValue("platform", out var platform) && platform is string p) req.Platform = p;
            if (data.TryGetValue("url", out var url) && url is string u) req._url = u;
            if (data.TryGetValue("hash", out var hash) && hash is string h) req._hash = h;
            if (data.TryGetValue("size", out var size) && size is uint si) req._size = si;
            return req;
        }
    }
}