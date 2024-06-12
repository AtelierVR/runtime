using System;
using Newtonsoft.Json.Linq;
using Nox.CCK;

namespace Nox.Mods
{
    public class Engine : CCK.Mods.Metadata.Engine
    {
        internal static Engine LoadFromJson(JObject json) => new()
        {
            _engine = EngineExtensions.GetEngineFromName(json["name"].Value<string>()),
            _version = new VersionMatching(json["version"].Value<string>())
        };
        internal static Engine LoadFromData(string key, string v) => new()
        {
            _engine = EngineExtensions.GetEngineFromName(key),
            _version = new VersionMatching(v)
        };

        public CCK.Engine GetName() => _engine;
        public VersionMatching GetVersion() => _version;


        private VersionMatching _version;
        private CCK.Engine _engine;
    }
}