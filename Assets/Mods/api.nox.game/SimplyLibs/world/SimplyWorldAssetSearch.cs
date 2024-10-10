using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using UnityEngine;

namespace Nox.SimplyLibs
{
    public class SimplyWorldAssetSearch : ShareObject
    {
        public SimplyWorldAsset[] assets;
        [ShareObjectImport, ShareObjectExport] public uint total;
        [ShareObjectImport, ShareObjectExport] public uint limit;
        [ShareObjectImport, ShareObjectExport] public uint offset;
        [ShareObjectImport, ShareObjectExport] public bool withEmpty;

        [ShareObjectImport, ShareObjectExport] public ShareObject[] SharedWorldAssets;
        [ShareObjectImport, ShareObjectExport] public Func<bool> SharedHasPrevious;
        public bool HasPrevious() => SharedHasPrevious();
        [ShareObjectImport, ShareObjectExport] public Func<bool> SharedHasNext;
        public bool HasNext() => SharedHasNext();
        [ShareObjectImport, ShareObjectExport] public Func<UniTask<ShareObject>> SharedPrevious;
        public async UniTask<SimplyWorldAssetSearch> Previous() => (await SharedPrevious())?.Convert<SimplyWorldAssetSearch>();
        [ShareObjectImport, ShareObjectExport] public Func<UniTask<ShareObject>> SharedNext;
        public async UniTask<SimplyWorldAssetSearch> Next() => (await SharedNext())?.Convert<SimplyWorldAssetSearch>();

        public void AfterImport()
        {
            assets = new SimplyWorldAsset[SharedWorldAssets.Length];
            for (int i = 0; i < SharedWorldAssets.Length; i++)
            {
                assets[i] = SharedWorldAssets[i].Convert<SimplyWorldAsset>();
            }
            SharedWorldAssets = null;
        }

        public void BeforeExport()
        {
            SharedWorldAssets = new ShareObject[assets.Length];
            for (int i = 0; i < assets.Length; i++)
                SharedWorldAssets[i] = assets[i];
            Debug.Log("BeforeExport" + SharedWorldAssets);
        }

        public override string ToString() => $"{GetType().Name}[total={total}, limit={limit}, offset={offset}, withEmpty={withEmpty}, hasPrevious={HasPrevious()}, hasNext={HasNext()}]";
    }
}