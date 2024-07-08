using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.Assets;
using Nox.CCK;
using Nox.Scripts;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Nox.Scenes
{
    public class SceneManager : Manager<Scene>
    {

        public static async UniTask<Scene> GotoFallback()
        {
            var config = Config.Load();
            var hash = config.Get("fallback_scene", "fallback");
            if (hash != "fallback") {
                var scene = await GotoAssetBundle(hash, 0);
                if (scene.IsValid() && scene.isLoaded) return scene;
            };
            var scenePathFallback = SceneUtility.GetScenePathByBuildIndex(1);
            if (scenePathFallback == null) return default;
            return await GotoPath(scenePathFallback);
        }

        public static async UniTask<Scene> GotoPath(string path, bool additive = false)
        {
            var operation = UnitySceneManager.LoadSceneAsync(path, additive ? LoadSceneMode.Additive : LoadSceneMode.Single);
            await UniTask.WaitUntil(() => operation.isDone);
            return UnitySceneManager.GetSceneByPath(path);
        }

        public static async UniTask<Scene> GotoAssetBundle(string hash, byte index, bool additive = false)
        {

            var asset = await AssetBundleManager.GetOrLoad(hash);
            if (asset == null) return default;
            asset.Tags.Add("scene");
            var scenes = asset.Value.GetAllScenePaths();
            if (scenes.Length <= index)
            {
                AssetBundleManager.Unload(hash);
                return default;
            }
            var scenePath = scenes[index];
            var result = await GotoPath(scenePath, additive);
            if (!result.isLoaded || !result.IsValid())
            {
                AssetBundleManager.Unload(hash);
                return default;
            }
            if (!additive)
                AssetBundleManager.UnloadGroup("scene", new List<string> { hash });
            return result;
        }
    }
}