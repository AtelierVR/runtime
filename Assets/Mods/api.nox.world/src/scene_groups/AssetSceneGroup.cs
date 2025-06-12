using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.CCK.Worlds;
using Nox.Worlds.Components;
using UnityEngine.SceneManagement;

namespace api.nox.world {
	public class AssetSceneGroup : SceneGroup {
		public static string ParseId(string ns, string path) {
			if (!string.IsNullOrEmpty(ns) && !string.IsNullOrEmpty(path))
				return $"asset:{ns}:{path}";
			Logger.LogError("Namespace or path cannot be null or empty.");
			return null;
		}

		public static async UniTask<AssetSceneGroup> Load(string ns, string path, Action<float> progress, CancellationToken token) {
			progress?.Invoke(0f);

			var scene = Main.Instance.CoreAPI.AssetAPI.GetWorld(ns, path);
			if (!scene.IsValid()) {
				var tmp = await Main.Instance.CoreAPI.AssetAPI.LoadWorld(ns, path, LoadSceneMode.Additive)
					.AttachExternalCancellation(token);

				if (token.IsCancellationRequested) {
					Logger.LogWarning($"Loading scene from AssetBundle {path} was cancelled before completion.");
					await Main.Instance.CoreAPI.AssetAPI.UnloadWorld(ns, path);
					return null;
				}

				scene = tmp;
			}

			if (!scene.IsValid()) {
				Logger.LogError($"Failed to load scene from AssetBundle: {path}");
				return null;
			}

			if (token.IsCancellationRequested) {
				Logger.LogWarning($"Loading scene from AssetBundle {path} was cancelled after loading.");
				await Main.Instance.CoreAPI.AssetAPI.UnloadWorld(ns, path);
				return null;
			}

			progress?.Invoke(0.6f);

			if (!BaseDescriptor.TryGetDescriptor<MainDescriptor>(scene, out var main)) {
				Logger.LogError($"Failed to load main descriptor: {path}");
				await Main.Instance.CoreAPI.AssetAPI.UnloadWorld(ns, path);
				return null;
			}

			var hidden = WorldHidden.Make(scene);
			if (!hidden) {
				Logger.LogError($"Failed to create WorldHidden for scene: {path}");
				await Main.Instance.CoreAPI.AssetAPI.UnloadWorld(ns, path);
				return null;
			}

			hidden.Set(false);
			progress?.Invoke(1f);

			var aw = new AssetSceneGroup {
				Id        = ParseId(ns, path),
				Active    = 0,
				SubScenes = new SubScene[main.GetScenes().Count]
			};

			aw.MainScene = new MainScene(aw, scene, main, hidden);
			return aw;
		}

		public override string ToString()
			=> $"{GetType().Name}[Id={Id} Active={Active}]";
	}
}