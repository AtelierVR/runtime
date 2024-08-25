using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class Instance : ShareObject
    {
        [ShareObjectExport] public uint id;
        [ShareObjectExport] public string title;
        [ShareObjectExport] public string description;
        [ShareObjectExport] public string server;
        [ShareObjectExport] public string name;
        [ShareObjectExport] public ushort capacity;
        [ShareObjectExport] public string[] tags;
        [ShareObjectExport] public string world;
    }
}