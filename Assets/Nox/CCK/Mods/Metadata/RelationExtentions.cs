namespace Nox.CCK.Mods.Metadata
{
    public class RelationExtensions
    {
        public static RelationType GetRelationTypeFromName(string name) => name switch
        {
            "depends" => RelationType.Depends,
            "recommends" => RelationType.Recommends,
            "suggests" => RelationType.Suggests,
            "breaks" => RelationType.Breaks,
            "conflicts" => RelationType.Conflicts,
            _ => RelationType.Depends
        };

        public static string GetRelationTypeFromEnum(RelationType type) => type switch
        {
            RelationType.Depends => "depends",
            RelationType.Recommends => "recommends",
            RelationType.Suggests => "suggests",
            RelationType.Breaks => "breaks",
            RelationType.Conflicts => "conflicts",
            _ => "depends"
        };
    }
}