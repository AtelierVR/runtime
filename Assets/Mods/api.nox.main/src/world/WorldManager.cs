/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using Logger = Nox.CCK.Utils.Logger;
using Nox.CCK.Utils;
using Nox.CCK.Worlds;

namespace api.nox.game.worlds
{
    public class WorldLock
    {
        public uint id;
        public string hash;
        public List<SceneLock> scenes = new();
    }

    public class SceneLock
    {
        public ushort index;
        public Scene scene;
    }

    public class LoadWorldSceneResult
    {
        public bool success;
        public string hash;
        public ushort id;
        public LoadSceneMode mode;
        public Scene scene;
        public string error;
    }

    public class LoadWorldSceneRequest
    {
        public string hash;
        public ushort id;
        public LoadSceneMode mode;
        public CancellationToken token;
        public Action<LoadSceneStage, float> progress;
    }

    public class ActionStageProgress : IProgress<float>
    {
        public Action<LoadSceneStage, float> action;
        public LoadSceneStage stage;
        public void Report(float value) => action?.Invoke(stage, value);
    }

    public enum LoadSceneStage
    {
        LoadAsset,
        LoadScene,
        Checking,
        Done
    }

    internal class WorldManager
    {
        public static Dictionary<string, AssetBundle> LoadedAssets = new();
        public static List<WorldLock> LockWorlds = new();

        /// <summary>
        /// Load an assetbundle from cache
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static async UniTask<AssetBundle> LoadAsset(string hash, Action<float> progress = null, CancellationToken token = default)
        {
            // If asset is already loaded, return it
            if (IsAssetLoaded(hash))
                return LoadedAssets[hash];

            // If asset is not in cache, return null
            if (!WorldCache.HasWorldInCache(hash))
                return null;

            // Load asset from cache
            var t0 = DateTime.Now;
            var promise = AssetBundle.LoadFromFileAsync(WorldCache.WorldPath(hash));

            Logger.LogDebug($"Starting to load assetbundle {hash}...");

            await UniTask.WaitUntil(() =>
            {
                progress?.Invoke(promise.progress);
                return promise.isDone || token.IsCancellationRequested;
            });

            // If loading is cancelled, return null
            if (token.IsCancellationRequested)
            {
                Logger.LogDebug($"Cancelled loading assetbundle {hash}");
                return null;
            }

            var t1 = DateTime.Now;

            Logger.LogDebug($"Loaded assetbundle {hash} in {(t1 - t0).TotalMilliseconds:0.000}ms");

            return LoadedAssets[hash] = promise.assetBundle;
        }

        /// <summary>
        /// Check if an assetbunle is locked
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static bool IsAssetLocked(string hash) => LockWorlds.Any(x => x.hash == hash);
        public static bool IsAssetLoaded(string hash) => LoadedAssets.ContainsKey(hash);

        /// <summary>
        /// Unload an assetbundle
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static bool UnloadAsset(string hash, bool force = false)
        {
            // If asset is not loaded, return true
            if (!IsAssetLoaded(hash))
                return true;

            // if asset is locked, return false
            if (IsAssetLocked(hash))
                return false;

            // Unload asset
            LoadedAssets[hash].Unload(true);
            LoadedAssets.Remove(hash);

            return true;
        }

        /// <summary>
        /// Unload all assetbundles
        /// </summary>
        /// <param name="force"></param>
        /// <returns></returns>
        public static bool UnloadAllAssets(bool force = false)
        {
            while (LockWorlds.Count > 0)
            {
                var world = LockWorlds[0];
                LockWorlds.RemoveAt(0);
                if (!UnloadAsset(world.hash, force))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Load a scene from an assetbundle
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static async UniTask<LoadWorldSceneResult> LoadScene(LoadWorldSceneRequest request)
        {
            if (!WorldCache.HasWorldInCache(request.hash))
                return new() { success = false, hash = request.hash, id = request.id, mode = request.mode, error = "World not in cache" };

            var asset = await LoadAsset(
                request.hash,
                p => request.progress?.Invoke(LoadSceneStage.LoadAsset, p),
                request.token
            );

            if (asset == null)
                return new() { success = false, hash = request.hash, id = request.id, mode = request.mode, error = "Failed to load asset" };

            var scenes = asset.GetAllScenePaths();
            if (scenes.Length <= request.id)
                return new() { success = false, hash = request.hash, id = request.id, mode = request.mode, error = "Scene not found" };

            // load new instance of scene
            var scene = await SceneManager.LoadScene(new()
            {
                sceneName = scenes[request.id],
                mode = request.mode,
                progress = (p) => request.progress?.Invoke(LoadSceneStage.LoadScene, p),
                token = request.token
            });

            if (!scene.success)
                return new() { success = false, hash = request.hash, id = request.id, mode = request.mode, error = scene.error };

            // get if the scene has a descriptor
            var descriptor = Finder.FindComponent<BaseDescriptor>(scene.scene);
            if (descriptor == null)
                return new() { success = false, hash = request.hash, id = request.id, mode = request.mode, error = "Scene has no descriptor" };

            // disable main camera
            var camera = Finder.FindComponent<Camera>(scene.scene);
            if (camera != null)
                camera.gameObject.SetActive(false);

            return new() { success = true, hash = request.hash, id = request.id, mode = request.mode, scene = scene.scene };
        }

        /// <summary>
        /// Unload a scene from an assetbundle
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static async UniTask<bool> UnloadScene(string hash, ushort id)
        {
            if (!WorldCache.HasWorldInCache(hash))
                return false;

            var asset = await LoadAsset(hash);
            if (asset == null)
                return false;

            var scenes = asset.GetAllScenePaths();
            if (scenes.Length <= id)
                return false;

            var scene = USceneManager.GetSceneByPath(scenes[id]);
            if (!scene.IsValid())
                return false;

            await USceneManager.UnloadSceneAsync(scene);

            UnloadAsset(hash);

            return true;
        }
    }
}*/