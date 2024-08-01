using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using UnityEngine;

namespace api.nox.network
{
    [System.Serializable]
    public class World : ShareObject
    {
        internal NetWorld netWorld;
        [ShareObjectExport] public uint id;
        [ShareObjectExport] public string title;
        [ShareObjectExport] public string description;
        [ShareObjectExport] public ushort capacity;
        [ShareObjectExport] public string[] tags;
        [ShareObjectExport] public string owner;
        [ShareObjectExport] public string server;
        [ShareObjectExport] public string thumbnail;

        [ShareObjectExport] public Func<uint, uint, uint[], string[], string[], bool, UniTask<ShareObject>> SharedSearchAssets;
        internal async UniTask<WorldAssetSearch> SearchAssets(uint offset = 0, uint limit = 10, uint[] versions = null, string[] platforms = null, string[] engines = null, bool withEmpty = false)
            => await netWorld.SearchAssets(server, id, offset, limit, versions, platforms, engines, withEmpty);

        [ShareObjectExport] public Func<uint, UniTask<ShareObject>> SharedGetAsset;
        internal async UniTask<WorldAsset> GetAsset(uint assetId)
            => await netWorld.GetAsset(server, id, assetId);

        public void BeforeExport()
        {
            SharedSearchAssets = async (offset, limit, versions, platforms, engines, withEmpty) => await SearchAssets(offset, limit, versions, platforms, engines, withEmpty);
            SharedGetAsset = async (assetId) => await GetAsset(assetId);
        }

        public void AfterImport()
        {
            SharedSearchAssets = null;
            SharedGetAsset = null;
        }
    }
}