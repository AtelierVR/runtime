using Nox.CCK.Mods;

namespace api.nox.game
{
    public class SimplyWorldAsset : ShareObject
    {
        [ShareObjectImport] public uint id;
        [ShareObjectImport] public uint version;
        [ShareObjectImport] public string engine;
        [ShareObjectImport] public string platform;
        [ShareObjectImport] public bool is_empty;
        [ShareObjectImport] public string url;
        [ShareObjectImport] public string hash;
        [ShareObjectImport] public uint size;
    }
}