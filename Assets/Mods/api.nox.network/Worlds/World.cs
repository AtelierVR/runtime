using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class World : ShareObject
    {
        [ShareObjectExport] public uint id;
        [ShareObjectExport] public string title;
        [ShareObjectExport] public string description;
        [ShareObjectExport] public ushort capacity;
        [ShareObjectExport] public string[] tags;
        [ShareObjectExport] public string owner;
        [ShareObjectExport] public string server;
        [ShareObjectExport] public string thumbnail;
        [ShareObjectExport] public WorldAsset[] assets;
    }
}