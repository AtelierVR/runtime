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

namespace api.nox.world
{
    public class WorldLock
    {
        public uint ID;
        public string Hash;
        public List<SceneLock> Scenes = new();
    }

    public class SceneLock
    {
        public ushort Index;
        public Scene Scene;
    }

    public class LoadWorldSceneResult
    {
        public bool Success;
        public string Hash;
        public ushort ID;
        public LoadSceneMode Mode;
        public Scene Scene;
        public string Error;
    }

    public class LoadWorldSceneRequest
    {
        public string Hash;
        public ushort ID;
        public LoadSceneMode Mode;
        public CancellationToken Token;
        public readonly Action<LoadSceneStage, float> Progress;

        public LoadWorldSceneRequest(Action<LoadSceneStage, float> progress, CancellationToken token)
        {
            Progress = progress;
            Token = token;
        }
    }

    public class ActionStageProgress : IProgress<float>
    {
        private readonly Action<LoadSceneStage, float> action;
        private readonly LoadSceneStage stage;

        public ActionStageProgress(Action<LoadSceneStage, float> action, LoadSceneStage stage)
        {
            this.action = action;
            this.stage = stage;
        }

        public void Report(float value) => action?.Invoke(stage, value);
    }

    public enum LoadSceneStage
    {
        LoadAsset,
        LoadScene,
        Checking,
        Done
    }

    public static class WorldLoader
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

            Logger.LogDebug($"Starting to load asset bundle {hash}...");

            await UniTask.WaitUntil(() =>
            {
                progress?.Invoke(promise.progress);
                return promise.isDone || token.IsCancellationRequested;
            }, cancellationToken: token);

            // If loading is cancelled, return null
            if (token.IsCancellationRequested)
            {
                Logger.LogDebug($"Cancelled loading asset bundle {hash}");
                return null;
            }

            var t1 = DateTime.Now;

            Logger.LogDebug($"Loaded asset bundle {hash} in {(t1 - t0).TotalMilliseconds:0.000}ms");

            return LoadedAssets[hash] = promise.assetBundle;
        }

        /// <summary>
        /// Check if an assetbunle is locked
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static bool IsAssetLocked(string hash) => LockWorlds.Any(x => x.Hash == hash);
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
                if (!UnloadAsset(world.Hash, force))
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
            if (!WorldCache.HasWorldInCache(request.Hash))
                return new() { Success = false, Hash = request.Hash, ID = request.ID, Mode = request.Mode, Error = "World not in cache" };

            var asset = await LoadAsset(
                request.Hash,
                p => request.Progress?.Invoke(LoadSceneStage.LoadAsset, p),
                request.Token
            );

            if (asset == null)
                return new() { Success = false, Hash = request.Hash, ID = request.ID, Mode = request.Mode, Error = "Failed to load asset" };

            var scenes = asset.GetAllScenePaths();
            if (scenes.Length <= request.ID)
                return new() { Success = false, Hash = request.Hash, ID = request.ID, Mode = request.Mode, Error = "Scene not found" };

            // load new instance of scene
            var scene = await SceneManager.LoadScene(new()
            {
                sceneName = scenes[request.ID],
                mode = request.Mode,
                progress = (p) => request.Progress?.Invoke(LoadSceneStage.LoadScene, p),
                token = request.Token
            });

            if (!scene.success)
                return new() { Success = false, Hash = request.Hash, ID = request.ID, Mode = request.Mode, Error = scene.error };

            // get if the scene has a descriptor
            var descriptor = Finder.FindComponent<BaseDescriptor>(scene.scene);
            if (descriptor == null)
                return new() { Success = false, Hash = request.Hash, ID = request.ID, Mode = request.Mode, Error = "Scene has no descriptor" };

            // disable main camera
            var camera = Finder.FindComponent<Camera>(scene.scene);
            if (camera != null)
                camera.gameObject.SetActive(false);

            return new() { Success = true, Hash = request.Hash, ID = request.ID, Mode = request.Mode, Scene = scene.scene };
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