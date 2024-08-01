using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;

namespace Nox.SimplyLibs
{
    public class SimplyWorldAPI : ShareObject
    {
        [ShareObjectImport] public Func<string, string, uint, uint, UniTask<ShareObject>> SharedSearchWorlds;
        [ShareObjectImport] public Func<string, uint[], UniTask<ShareObject[]>> SharedGetWorlds;
        [ShareObjectImport] public Func<string, uint, uint, uint, uint[], string[], string[], bool, UniTask<ShareObject>> SharedSearchAssets;
        [ShareObjectImport] public Func<string, uint, uint, UniTask<ShareObject>> SharedGetAsset;
        [ShareObjectImport] public Func<string, uint, UniTask<ShareObject>> SharedGetWorld;
        [ShareObjectImport] public Func<ShareObject, UniTask<ShareObject>> SharedCreateWorld;
        [ShareObjectImport] public Func<ShareObject, UniTask<ShareObject>> SharedCreateAsset;
        [ShareObjectImport] public Func<string, uint, uint, string, UniTask<bool>> SharedUploadAssetFile;

        public async UniTask<SimplyWorldSearch> SearchWorlds(string server, string query, uint offset = 0, uint limit = 10)
            => (await SharedSearchWorlds(server, query, offset, limit))?.Convert<SimplyWorldSearch>();

        public async UniTask<SimplyWorld[]> GetWorlds(string server, params uint[] ids)
        {
            var worlds = await SharedGetWorlds(server, ids);
            var result = new SimplyWorld[worlds.Length];
            for (var i = 0; i < worlds.Length; i++)
                result[i] = worlds[i].Convert<SimplyWorld>();
            return result;
        }

        public async UniTask<SimplyWorldAssetSearch> SearchAssets(string server, uint worldId, uint offset = 0, uint limit = 10, uint[] versions = null, string[] platforms = null, string[] engines = null, bool withEmpty = false)
            => (await SharedSearchAssets(server, worldId, offset, limit, versions, platforms, engines, withEmpty))?.Convert<SimplyWorldAssetSearch>();

        public async UniTask<SimplyWorldAsset> GetAsset(string server, uint worldId, uint assetId)
            => (await SharedGetAsset(server, worldId, assetId))?.Convert<SimplyWorldAsset>();

        public async UniTask<SimplyWorld> GetWorld(string server, uint worldId)
            => (await SharedGetWorld(server, worldId))?.Convert<SimplyWorld>();

        public async UniTask<SimplyWorld> CreateWorld(SimplyCreateWorldData data)
            => (await SharedCreateWorld(data))?.Convert<SimplyWorld>();

        public async UniTask<SimplyWorldAsset> CreateAsset(SimplyCreateAssetData data)
            => (await SharedCreateAsset(data))?.Convert<SimplyWorldAsset>();

        public async UniTask<bool> UploadAssetFile(string server, uint worldId, uint assetId, string path)
            => await SharedUploadAssetFile(server, worldId, assetId, path);
    }


}