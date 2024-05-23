using Newtonsoft.Json.Linq;

namespace Nox.Mods
{
    public class Reference : CCK.Mods.Metadata.Reference
    {
        public static Reference LoadFromJson(JToken json) => new()
        {
            _name = json["namespace"].Value<string>(),
            _file = json["file"].Value<string>(),
            _url = json["url"].Value<string>()
        };

        public string GetFile() => _file;
        public string GetNamespace() => _name;
        public string GetUrl() => _url;

        private string _name;
        private string _file;
        private string _url;
    }
}