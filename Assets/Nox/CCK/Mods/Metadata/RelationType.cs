namespace Nox.CCK.Mods.Metadata
{
    public enum RelationType
    {
        Depends, // important requirement
        Recommends, // recommanded (more than suggests) requirement
        Suggests, // suggests requirement
        Conflicts, // disable thte current mod if detect a conflict mod
        Breaks // crash game if the mod are detected
    }
}