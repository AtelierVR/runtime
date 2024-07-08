using System.Linq;
using Newtonsoft.Json.Linq;
using Nox.CCK;
using Nox.CCK.Mods.Metadata;

namespace Nox.Mods
{
    public class Relation : CCK.Mods.Metadata.Relation
    {
        internal static Relation LoadFromJson(string key, JObject json) => new()
        {
            _id = key,
            _relationType = json.TryGetValue("type", out var type) ? RelationExtensions.GetRelationTypeFromName(type.Value<string>()) : CCK.Mods.Metadata.RelationType.Depends,
            _version = json.TryGetValue("version", out var version) ? new VersionMatching(version.Value<string>()) : new VersionMatching(">=0.0.0")
        };

        internal static Relation LoadFromData(string id, string version) => new()
        {
            _id = id,
            _relationType = CCK.Mods.Metadata.RelationType.Depends,
            _version = new VersionMatching(version)
        };

        public string GetId() => _id;

        public CCK.Mods.Metadata.RelationType GetRelationType() => _relationType;

        public VersionMatching GetVersion() => _version;

        private VersionMatching _version;
        private CCK.Mods.Metadata.RelationType _relationType;
        private string _id;
    }
}