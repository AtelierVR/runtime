using System.Configuration.Assemblies;

namespace Nox.CCK.Mods.Metadata
{
    public interface Relation
    {
        string GetId();
        Utils.VersionMatching GetVersion();
        RelationType GetRelationType();
    }
}