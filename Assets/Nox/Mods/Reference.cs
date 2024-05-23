using Newtonsoft.Json.Linq;

namespace Nox.Mods
{
    public class Reference : CCK.Mods.Metadata.Reference
    {
        internal static Reference LoadFromJson(JObject json) => new()
        {
            _name = json.TryGetValue("name", out var name) ? name.Value<string>() : null,
            _file = json.TryGetValue("file", out var file) ? file.Value<string>() : null,
            _url = json.TryGetValue("url", out var url) ? url.Value<string>() : null
        };

        public string GetFile() => _file;
        public string GetNamespace() => _name;
        public string GetUrl() => _url;

        private string _name;
        private string _file;
        private string _url;
    }
}