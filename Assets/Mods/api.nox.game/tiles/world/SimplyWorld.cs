using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace api.nox.game
{
    public class SimplyWorld : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public uint id;
        [ShareObjectImport, ShareObjectExport] public string title;
        [ShareObjectImport, ShareObjectExport] public string description;
        [ShareObjectImport, ShareObjectExport] public ushort capacity;
        [ShareObjectImport, ShareObjectExport] public string[] tags;
        [ShareObjectImport, ShareObjectExport] public string owner;
        [ShareObjectImport, ShareObjectExport] public string server;
        [ShareObjectImport, ShareObjectExport] public string thumbnail;

        [ShareObjectImport, ShareObjectExport] public Func<uint, uint, uint[], string[], string[], UniTask<ShareObject>> SharedSearchAssets;
        public async UniTask<SimplyWorldAssetSearch> SearchAssets(uint offset = 0, uint limit = 10, uint[] versions = null, string[] platforms = null, string[] engines = null)
            => (await SharedSearchAssets(offset, limit, versions, platforms, engines))?.Convert<SimplyWorldAssetSearch>();
    }
}