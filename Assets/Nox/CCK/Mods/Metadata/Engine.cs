namespace Nox.CCK.Mods.Metadata
{
    public interface Engine
    {
        CCK.Engine GetName();
        VersionMatching GetVersion();
    }
}