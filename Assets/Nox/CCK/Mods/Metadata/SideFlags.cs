using System;

namespace Nox.CCK.Mods.Metadata
{
    [Flags]
    public enum SideFlags
    {
        None = 0,
        Client = 1,
        Instance = 2,
        Editor = 4,
    }
}