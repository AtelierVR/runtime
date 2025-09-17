using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Worlds;
using Nox.CCK.Build;
using Nox.CCK.Utils;
using Nox.Worlds.Scenes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.world {
	public class WorldSetup {
		public class PrepareResult<T> where T : RuntimeWorldGroup {
			public bool   Success;
			public string Error;
			public T      Runtime;
		}

		public static async UniTask<PrepareResult<T>> Prepare<T>(Scene scene, Action<float> progress = null, CancellationToken token = default) where T : RuntimeWorldGroup, new() {
			var runtime = new T { Active = 0 };
			if (!scene.IsValid())
				return new PrepareResult<T> {
					Success = false,
					Error   = "Invalid scene."
				};

			progress?.Invoke(0.0f);

			var prefab = new GameObject($"[Reference] {nameof(T)}");
			SceneManager.MoveGameObjectToScene(prefab, scene);
			foreach (var root in scene.GetRootGameObjects())
				root.transform.SetParent(prefab.transform);
			prefab.SetActive(false);

			if (!prefab.TryGetComponentInChildren<IWorldDescriptor>(out var descriptor))
				return new PrepareResult<T> {
					Success = false,
					Error   = "No world descriptor found in scene."
				};

			// Vérifier l'annulation dès le début
			if (token.IsCancellationRequested)
				return new PrepareResult<T> {
					Success = false,
					Error   = "Operation cancelled."
				};

			var gameObject = descriptor.GetAnchor();

			if (!gameObject)
				return new PrepareResult<T> {
					Success = false,
					Error   = "World descriptor root GameObject is null."
				};

			descriptor.FindModules();

			var valid = true;
			Main.Instance.CoreAPI.EventAPI.Emit("world_check_request", descriptor, new Action<object[]>(OnCheckRequest));

			if (!valid)
				return new PrepareResult<T> {
					Success = false,
					Error   = "A mod asked to cancel the world preparation."
				};

			descriptor.FindModules();

			if (token.IsCancellationRequested)
				return new PrepareResult<T> {
					Success = false,
					Error   = "Operation cancelled."
				};

			progress?.Invoke(0.1f);

			var compilable = gameObject
				.GetComponentsInChildren<ICompilable>(true)
				.OrderBy(c => c.CompileOrder)
				.ToArray();

			// Compilation des composants avec progression
			for (var i = 0; i < compilable.Length; i++) {
				if (token.IsCancellationRequested)
					return new PrepareResult<T> {
						Success = false,
						Error   = "Operation cancelled."
					};

				var c = compilable[i];
				if (c == null) {
					Logger.LogWarning($"Compilable component at index {i} is null, skipping.");
					continue;
				}

				Logger.LogDebug($"Compiling {c.GetType().Name} ({i + 1}/{compilable.Length})...");
				c.Compile();
				await c.CompileAsync();

				// Rapporter la progression (10% à 70% pour la compilation)
				var compileProgress = 0.2f + 0.5f * (i + 1) / compilable.Length;
				progress?.Invoke(compileProgress);
			}

			if (token.IsCancellationRequested)
				return new PrepareResult<T> {
					Success = false,
					Error   = "Operation cancelled."
				};

			progress?.Invoke(0.8f);

			var modules     = descriptor.GetModules();
			var moduleArray = modules.ToArray();

			// Initialisation des modules avec progression
			for (var i = 0; i < moduleArray.Length; i++) {
				if (token.IsCancellationRequested)
					return new PrepareResult<T> {
						Success = false,
						Error   = "Operation cancelled."
					};

				if (!await moduleArray[i].Setup(runtime))
					return new PrepareResult<T> {
						Success = false,
						Error   = $"Module {moduleArray[i].GetType().Name} failed to initialize."
					};

				// Rapporter la progression (80% à 100% pour les modules)
				var moduleProgress = 0.8f + 0.2f * (i + 1) / moduleArray.Length;
				progress?.Invoke(moduleProgress);
			}

			var scenes = descriptor.GetModules<IScenesModule>().FirstOrDefault();
			if (scenes == null)
				return new PrepareResult<T> {
					Success = false,
					Error   = "No scenes module found in world descriptor."
				};

			foreach (var camera in prefab.GetComponentsInChildren<Camera>(true))
				if (camera.CompareTag("MainCamera"))
					camera.tag = "Untagged";
			
			foreach (var eventSystem in prefab.GetComponentsInChildren<EventSystem>(true))
				eventSystem.enabled = false;

			runtime.Instances    = new RuntimeWorldInstance[scenes.GetScenes().Length + 1];
			runtime.Instances[0] = new RuntimeWorldInstance(runtime, scene, prefab);

			progress?.Invoke(1.0f);
			return new PrepareResult<T> {
				Success = true,
				Error   = null,
				Runtime = runtime
			};

			void OnCheckRequest(object[] args) {
				if (args.Length > 0 && args[0] is false)
					valid = false;
			}
		}
	}
}