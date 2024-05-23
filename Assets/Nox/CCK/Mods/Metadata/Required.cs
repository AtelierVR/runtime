namespace Nox.CCK.Mods.Metadata
{
    public interface Required
    {
        bool ForClient(); // If set to false and the mod is universal, clients don't need the mod to join.
        bool ForInstance(); // If set to false and the mod is universal, the mod is not disabled if it's not present on the server.
    }
}