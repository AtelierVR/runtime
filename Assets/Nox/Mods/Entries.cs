using Newtonsoft.Json.Linq;
using Nox.CCK;

namespace Nox.Mods
{
    public class Entries : CCK.Mods.Metadata.Entries
    {
        internal static Entries LoadFromJson(JObject json) => new()
        {
            _main = json.TryGetValue("main", out var main) ? main.ToObject<string[]>() : new string[0],
            _client = json.TryGetValue("client", out var client) ? client.ToObject<string[]>() : new string[0],
            _instance = json.TryGetValue("instance", out var instance) ? instance.ToObject<string[]>() : new string[0],
            _editor = json.TryGetValue("editor", out var editor) ? editor.ToObject<string[]>() : new string[0]
        };

        internal Entries()
        {
            _main = new string[0];
            _client = new string[0];
            _instance = new string[0];
            _editor = new string[0];
        }

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