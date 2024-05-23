using Newtonsoft.Json.Linq;
using Nox.CCK;

namespace Nox.Mods
{
    public class Entries : CCK.Mods.Metadata.Entries
    {
        public static Entries LoadFromJson(JToken json) => new()
        {
            _main = json["main"].ToObject<string[]>(),
            _client = json["client"].ToObject<string[]>(),
            _instance = json["instance"].ToObject<string[]>(),
            _editor = json["editor"].ToObject<string[]>()
        };

        public string[] GetClient() => _client;

        public string[] GetEditor() => _editor;

        public string[] GetInstance() => _instance;

        public string[] GetMain() => _main;

        private string[] _main;
        private string[] _client;
        private string[] _instance;
        private string[] _editor;
    }
}