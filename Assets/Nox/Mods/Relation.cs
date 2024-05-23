using System.Linq;
using Newtonsoft.Json.Linq;
using Nox.CCK;

namespace Nox.Mods
{
    public class Relation : CCK.Mods.Metadata.Relation
    {
        public static Relation LoadFromJson(JToken json) => new()
        {
            _id = json["id"].Value<string>(),
            _relationType = RelationExtensions.GetRelationTypeFromName(json["relationType"].Value<string>()),
            _version = new VersionMatching(json["version"].Value<string>())
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