
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using UnityEngine;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using Logger = Nox.CCK.Logger;
using Nox.CCK.Worlds;

namespace api.nox.game.Worlds
{
    public class LoadSceneResult
    {
        public bool success;
        public string sceneName;
        public string error;
        public Scene scene;
    }

    public class LoadSceneRequest
    {
        public string sceneName;
        public LoadSceneMode mode;
        public Action<float> progress;
        public CancellationToken token;
    }

    public class ActionProgress : IProgress<float>
    {
        public Action<float> action;
        public void Report(float value) => action?.Invoke(value);
    }

    public class SceneManager
    {
        public static async UniTask<LoadSceneResult> LoadScene(LoadSceneRequest request)
        {
            // Load scene
            var t0 = DateTime.Now;

            await USceneManager.LoadSceneAsync(request.sceneName, request.mode).ToUniTask(
                progress: new ActionProgress { action = request.progress },
                cancellationToken: request.token
            );

            if (request.token.IsCancellationRequested)
                return new() { success = false, sceneName = request.sceneName, error = "Cancelled" };

            var t1 = DateTime.Now;

            Logger.Log($"Loaded scene {request.sceneName} in {(t1 - t0).TotalMilliseconds}ms");

            // Check if scene was loaded
            var scene = USceneManager.GetSceneByPath(request.sceneName);

            if (!scene.IsValid())
                return new() { success = false, sceneName = request.sceneName, error = "Scene not found" };

            // hid main camera of the scene
            var mainCamera = GameObject.Find("Main Camera");
            if (mainCamera != null)
                mainCamera.SetActive(false);

            WorldHidden.Make(scene);

            return new()
            {
                success = true,
                sceneName = request.sceneName,
                scene = scene
            };
        }
    }
}
