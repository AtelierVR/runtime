using Newtonsoft.Json.Linq;
using Nox.CCK;

namespace Nox.Mods
{
    public class Reference : CCK.Mods.Metadata.Reference
    {
        internal static Reference LoadFromJson(JObject json) => new()
        {
            _name = json.TryGetValue("name", out var name) ? name.Value<string>() : null,
            _file = json.TryGetValue("file", out var file) ? file.Value<string>() : null,
            _engine = json.TryGetValue("engine", out var engine) ? Engine.LoadFromJson(engine.Value<JObject>()) : null,
            _platform = json.TryGetValue("platform", out var platform) ? PlatfromExtensions.GetPlatformFromName(platform.Value<string>()) : CCK.Platfrom.None
        };

        public string GetFile() => _file;
        public string GetNamespace() => _name;

        public CCK.Mods.Metadata.Engine GetEngine() => _engine;
        public Platfrom GetPlatform() => _platform;

        private string _name;
        private string _file;
        private Engine _engine;
        private Platfrom _platform;

    }
}