using Nox.CCK.Mods;

namespace api.nox.network
{
    [System.Serializable]
    public class WorldAsset : ShareObject
    {
        internal NetWorld netWorld;
        [ShareObjectExport] public uint id;
        [ShareObjectExport] public uint version;
        [ShareObjectExport] public string engine;
        [ShareObjectExport] public string platform;
        [ShareObjectExport] public bool is_empty;
        [ShareObjectExport] public string url;
        [ShareObjectExport] public string hash;
        [ShareObjectExport] public uint size;
    }
}