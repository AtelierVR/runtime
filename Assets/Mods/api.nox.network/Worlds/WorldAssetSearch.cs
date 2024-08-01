using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using UnityEngine;

namespace api.nox.network
{
    [System.Serializable]
    public class WorldAssetSearch : ShareObject
    {
        internal NetWorld netWorld;
        internal uint[] versions;
        internal string[] platforms;
        internal string[] engines;
        internal string server;
        internal uint world_id;
        public WorldAsset[] assets;
        [ShareObjectExport] public uint total;
        [ShareObjectExport] public uint limit;
        [ShareObjectExport] public uint offset;
        [ShareObjectExport] public bool withEmpty;
        public bool HasPrevious() => offset > 0;
        public bool HasNext() => offset + limit < total;
        public async UniTask<WorldAssetSearch> Previous()
            => HasPrevious() ? await netWorld.SearchAssets(server, world_id, offset - limit, limit, versions, platforms, engines, withEmpty) : null;
        public async UniTask<WorldAssetSearch> Next()
            => HasNext() ? await netWorld.SearchAssets(server, world_id, offset + limit, limit, versions, platforms, engines, withEmpty) : null;

        [ShareObjectExport] public ShareObject[] SharedWorldAssets;
        [ShareObjectExport] public Func<bool> SharedHasPrevious;
        [ShareObjectExport] public Func<bool> SharedHasNext;
        [ShareObjectExport] public Func<UniTask<ShareObject>> SharedPrevious;
        [ShareObjectExport] public Func<UniTask<ShareObject>> SharedNext;

        public void BeforeExport()
        {
            SharedWorldAssets = new ShareObject[assets.Length];
            for (var i = 0; i < assets.Length; i++)
                SharedWorldAssets[i] = assets[i];
            Debug.Log("BeforeExport" + SharedWorldAssets + " " + assets.Length);
            SharedHasPrevious = HasPrevious;
            SharedHasNext = HasNext;
            SharedPrevious = async () => await Previous();
            SharedNext = async () => await Next();
        }

        public void AfterExport()
        {
            SharedWorldAssets = null;
            SharedHasPrevious = null;
            SharedHasNext = null;
            SharedPrevious = null;
        }
    }
}