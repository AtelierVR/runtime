using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.CCK.Worlds;
using Nox.Worlds;
using Nox.Worlds.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.world {
	public class AssetRuntimeWorldGroup : RuntimeWorldGroup {
		public static string ParseId(string ns, string path) {
			if (!string.IsNullOrEmpty(ns) && !string.IsNullOrEmpty(path))
				return $"asset:{ns}:{path}";
			Logger.LogError("Namespace or path cannot be null or empty.");
			return null;
		}

		public static async UniTask<AssetRuntimeWorldGroup> Load(string ns, string path, Action<float> progress, CancellationToken token) {
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
				Logger.LogError($"Failed to load scene from Internal Assets: {path}");
				return null;
			}

			if (token.IsCancellationRequested) {
				Logger.LogWarning($"Loading scene from AssetBundle {path} was cancelled after loading.");
				await Main.Instance.CoreAPI.AssetAPI.UnloadWorld(ns, path);
				return null;
			}
			
			progress?.Invoke(0.6f);

			var res = await WorldSetup.Prepare<AssetRuntimeWorldGroup>(
				scene,
				progress: p => progress?.Invoke(0.6f + p * 0.4f),
				token: token
			);

			if (!res.Success) {
				Logger.LogError($"Failed to prepare world from AssetBundle: {path} ({res.Error})");
				await Main.Instance.CoreAPI.AssetAPI.UnloadWorld(ns, path);
				return null;
			}

			res.Runtime.Id = ParseId(ns, path);

			progress?.Invoke(1f);

			return res.Runtime;
		}

		public override string ToString()
			=> $"{GetType().Name}[Id={Id} Active={Active}]";
	}
}