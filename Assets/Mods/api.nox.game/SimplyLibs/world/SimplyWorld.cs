using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
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

        [ShareObjectImport, ShareObjectExport] public Func<uint, uint, uint[], string[], string[], bool, UniTask<ShareObject>> SharedSearchAssets;
        public async UniTask<SimplyWorldAssetSearch> SearchAssets(uint offset = 0, uint limit = 10, uint[] versions = null, string[] platforms = null, string[] engines = null, bool withEmpty = false)
            => (await SharedSearchAssets(offset, limit, versions, platforms, engines, withEmpty))?.Convert<SimplyWorldAssetSearch>();

        [ShareObjectImport, ShareObjectExport] public Func<uint, UniTask<ShareObject>> SharedGetAsset;
        public async UniTask<SimplyWorldAsset> GetAsset(uint assetId)
            => (await SharedGetAsset(assetId))?.Convert<SimplyWorldAsset>();
    }
}