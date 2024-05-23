using Newtonsoft.Json.Linq;
using Nox.CCK;

namespace Nox.Mods
{
    public class Engine : CCK.Mods.Metadata.Engine
    {
        public static Engine LoadFromJson(JToken json) => new()
        {
            _engine = EngineExtensions.GetEngineFromName(json["name"].Value<string>()),
            _version = new VersionMatching(json["version"].Value<string>())
        };

        public CCK.Engine GetName() => _engine;
        public VersionMatching GetVersion() => _version;

        private VersionMatching _version;
        private CCK.Engine _engine;
    }
}