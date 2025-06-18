using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Worlds;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.world {
	public class AssetBundleSceneGroup : SceneGroup {
		public AssetBundle AssetBundle;


		public static string ParseId(string path) {
			if (!string.IsNullOrEmpty(path))
				return $"asset_bundle:{path}";
			Logger.LogError("AssetBundleWorld: Path is null or empty.");
			return null;
		}

		public override async UniTask Dispose() {
			await base.Dispose();

			if (AssetBundle) {
				await AssetBundle.UnloadAsync(true);
				AssetBundle = null;
			}
		}

		public static async UniTask<AssetBundleSceneGroup> Load(string path, Action<float> progress, CancellationToken token) {
			progress?.Invoke(0f);

			if (string.IsNullOrEmpty(path)) {
				Logger.LogError("AssetBundleWorld: Path is null or empty.");
				return null;
			}

			if (token.IsCancellationRequested) {
				Logger.LogWarning($"Loading AssetBundle {path} was cancelled.");
				return null;
			}

			var bundle = await AssetBundle.LoadFromFileAsync(path)
				.ToUniTask(
					progress: new Progress<float>(p => progress?.Invoke(p * 0.3f)),
					cancellationToken: token
				);

			if (!bundle) {
				Logger.LogError($"Failed to load AssetBundle from path: {path}");
				return null;
			}

			if (token.IsCancellationRequested) {
				Logger.LogWarning($"Loading AssetBundle {path} was cancelled after loading.");
				await bundle.UnloadAsync(true);
				return null;
			}

			progress?.Invoke(0.3f);

			var scenes = bundle.GetAllScenePaths();
			if (scenes.Length == 0 || string.IsNullOrEmpty(scenes[0])) {
				Logger.LogError($"No scenes found in AssetBundle: {path}");
				await bundle.UnloadAsync(true);
				return null;
			}

			await SceneManager.LoadSceneAsync(scenes[0], LoadSceneMode.Additive)
				.ToUniTask(
					progress: new Progress<float>(p => progress?.Invoke(p * 0.3f + 0.3f)),
					cancellationToken: token
				);

			var scene = SceneManager.GetSceneByPath(scenes[0]);

			if (!scene.IsValid()) {
				Logger.LogError($"Failed to load scene from AssetBundle: {path}");
				await bundle.UnloadAsync(true);
				return null;
			}

			if (token.IsCancellationRequested) {
				Logger.LogWarning($"Loading scene from AssetBundle {path} was cancelled after loading.");
				await SceneManager.UnloadSceneAsync(scene);
				await bundle.UnloadAsync(true);
				return null;
			}

			progress?.Invoke(0.6f);

			var prefab = new GameObject($"[Reference] {nameof(AssetBundleSceneGroup)}");
			SceneManager.MoveGameObjectToScene(prefab, scene);
			foreach (var root in scene.GetRootGameObjects())
				root.transform.SetParent(prefab.transform);
			prefab.SetActive(false);

			if (!SceneDescriptorExtension.TryGetDescriptor<MainSceneDescriptor>(scene, out var main)) {
				Logger.LogError($"Failed to load main descriptor: {path}");
				await SceneManager.UnloadSceneAsync(scene);
				await bundle.UnloadAsync(true);
				return null;
			}

			progress?.Invoke(1f);

			var abw = new AssetBundleSceneGroup {
				Id          = ParseId(path),
				AssetBundle = bundle,
				Active      = 0,
				SubScenes   = new SubScene[main.GetScenes().Length]
			};

			abw.MainScene = new MainScene(abw, scene, prefab);
			return abw;
		}

		public override string ToString()
			=> $"{GetType().Name}[Id={Id} Active={Active} AssetBundle={AssetBundle.name}]";
	}
}