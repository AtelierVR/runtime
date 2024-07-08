using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Scripts;
using UnityEngine;
using CacheManager = Nox.CCK.Cache;
using UnityAssetBundle = UnityEngine.AssetBundle;

namespace Nox.Assets
{
    public class AssetBundleManager : Manager<AssetBundle>
    {
        public static AssetBundle Get(string hash)
        {
            foreach (var asset in Cache)
                if (asset.Id == hash)
                    return asset;
            return null;
        }

        public static async UniTask<AssetBundle> GetOrLoad(string hash)
        {
            var asset = Get(hash);
            if (asset != null)
                return asset;
            var bundle = await Load(hash);
            if (bundle == null)
                return null;
            Add(bundle);
            return bundle;
        }

        public static async UniTask<AssetBundle> Load(string hash)
        {
            if (!CacheManager.Has(hash)) return null;
            var path = CacheManager.GetPath(hash);
            var bundle = await UnityAssetBundle.LoadFromFileAsync(path);
            if (bundle == null) return null;
            Debug.Log($"Loaded asset bundle {hash}");
            return new AssetBundle(hash, bundle);
        }

        public static void Unload(string hash)
        {
            var asset = Get(hash);
            if (asset == null) return;
            asset.Value.Unload(true);
            Remove(asset);
            Debug.Log($"Unloaded asset bundle {hash}");
        }

        public static void UnloadAll()
        {
            foreach (var asset in Cache)
                asset.Value.Unload(true);
            Clear();
        }

        public static void UnloadGroup(string group, List<string> execpt)
        {
            foreach (var asset in Cache.ToList())
                if (asset.Tags.Contains(group) && !execpt.Contains(asset.Id))
                {
                    asset.Value.Unload(true);
                    Remove(asset);
                }
        }
    }
}