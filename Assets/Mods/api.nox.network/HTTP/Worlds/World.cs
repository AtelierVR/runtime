using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using UnityEngine;

namespace api.nox.network
{
    [System.Serializable]
    public class World : ShareObject
    {
        internal NetworkSystem networkSystem;
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
            => await networkSystem._world.SearchAssets(server, id, offset, limit, versions, platforms, engines, withEmpty);

        [ShareObjectExport] public Func<uint, UniTask<ShareObject>> SharedGetAsset;
        internal async UniTask<WorldAsset> GetAsset(uint assetId)
            => await networkSystem._world.GetAsset(server, id, assetId);

        [ShareObjectExport] public Func<string, string, bool> SharedMatch;
        public bool Match(string reference, string default_server)
            => new WorldIdentifier(id, server).ToMinimalString() == WorldIdentifier.FromString(reference).ToMinimalString(default_server);

        [ShareObjectExport] public Func<string, string> SharedToMinimalString;
        public string ToMinimalString(string default_server = null) => new WorldIdentifier(id, server).ToMinimalString(default_server);

        [ShareObjectExport] public Func<string, string> SharedToFullString;
        public string ToFullString(string default_server = null) => new WorldIdentifier(id, server).ToFullString(default_server);

        public void BeforeExport()
        {
            SharedSearchAssets = async (offset, limit, versions, platforms, engines, withEmpty) => await SearchAssets(offset, limit, versions, platforms, engines, withEmpty);
            SharedGetAsset = async (assetId) => await GetAsset(assetId);
            SharedMatch = (reference, default_server) => Match(reference, default_server);
            SharedToMinimalString = (default_server) => ToMinimalString(default_server);
            SharedToFullString = (default_server) => ToFullString(default_server);
        }

        public void AfterImport()
        {
            SharedSearchAssets = null;
            SharedGetAsset = null;
            SharedMatch = null;
            SharedToMinimalString = null;
            SharedToFullString = null;
        }

    }
}