using System.Linq;
using Newtonsoft.Json.Linq;
using Nox.CCK;

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

    public class RelationExtensions
    {
        public static CCK.Mods.Metadata.RelationType GetRelationTypeFromName(string name) => name switch
        {
            "depends" => CCK.Mods.Metadata.RelationType.Depends,
            "recommends" => CCK.Mods.Metadata.RelationType.Recommends,
            "suggests" => CCK.Mods.Metadata.RelationType.Suggests,
            "breaks" => CCK.Mods.Metadata.RelationType.Breaks,
            "conflicts" => CCK.Mods.Metadata.RelationType.Conflicts,
            _ => CCK.Mods.Metadata.RelationType.Depends
        };

        public static string GetRelationTypeFromEnum(CCK.Mods.Metadata.RelationType type) => type switch
        {
            CCK.Mods.Metadata.RelationType.Depends => "depends",
            CCK.Mods.Metadata.RelationType.Recommends => "recommends",
            CCK.Mods.Metadata.RelationType.Suggests => "suggests",
            CCK.Mods.Metadata.RelationType.Breaks => "breaks",
            CCK.Mods.Metadata.RelationType.Conflicts => "conflicts",
            _ => "depends"
        };
    }
}