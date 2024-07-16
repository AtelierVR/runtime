using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using UnityEngine;

namespace api.nox.game
{
    public class SimplyWorldAssetSearch : ShareObject
    {
        public SimplyWorldAsset[] assets;
        [ShareObjectImport] public uint total;
        [ShareObjectImport] public uint limit;
        [ShareObjectImport] public uint offset;

        [ShareObjectImport] public ShareObject[] SharedWorldAssets;
        [ShareObjectImport] public Func<bool> SharedHasPrevious;
        public bool HasPrevious() => SharedHasPrevious();
        [ShareObjectImport] public Func<bool> SharedHasNext;
        public bool HasNext() => SharedHasNext();
        [ShareObjectImport] public Func<UniTask<ShareObject>> SharedPrevious;
        public async UniTask<SimplyWorldAssetSearch> Previous() => (await SharedPrevious())?.Convert<SimplyWorldAssetSearch>();
        [ShareObjectImport] public Func<UniTask<ShareObject>> SharedNext;
        public async UniTask<SimplyWorldAssetSearch> Next() => (await SharedNext())?.Convert<SimplyWorldAssetSearch>();

        public void AfterImport()
        {
            assets = new SimplyWorldAsset[SharedWorldAssets.Length];
            for (int i = 0; i < SharedWorldAssets.Length; i++)
                assets[i] = SharedWorldAssets[i].Convert<SimplyWorldAsset>();
            SharedWorldAssets = null;
        }
    }
}