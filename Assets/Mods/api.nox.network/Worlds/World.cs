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

        [ShareObjectExport] public Func<uint, uint, uint[], string[], string[], UniTask<ShareObject>> SharedSearchAssets;
        internal async UniTask<WorldAssetSearch> SearchAssets(uint offset = 0, uint limit = 10, uint[] versions = null, string[] platforms = null, string[] engines = null)
            => await netWorld.SearchAssets(server, id, offset, limit, versions, platforms, engines);

        public void BeforeExport()
        {
            Debug.Log("Exporting world " + id);
            SharedSearchAssets = async (offset, limit, versions, platforms, engines) => await SearchAssets(offset, limit, versions, platforms, engines);
        }

        public void AfterImport()
        {
            SharedSearchAssets = null;
        }


    }
}