using System.Configuration.Assemblies;

namespace Nox.CCK.Mods.Metadata
{
    public interface Relation
    {
        string GetId();
        VersionMatching GetVersion();
        RelationType GetRelationType();
    }
}