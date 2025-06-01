using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace api.nox.world {
	public class WorldManager : INoxObject {
		public readonly List<BaseWorld> Worlds = new();

		internal readonly UnityEvent<BaseWorld> OnWorldAdded   = new();
		internal readonly UnityEvent<BaseWorld> OnWorldRemoved = new();

		public async UniTask Dispose() {
			foreach (var world in Worlds) {
				await world.Dispose();
				OnWorldRemoved.Invoke(world);
			}

			Worlds.Clear();
		}

		[NoxPublic(NoxAccess.Method)]
		public BaseWorld GetWorld(string id)
			=> Worlds.Find(w => w.Id == id);

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<AssetBundleWorld> LoadWorldFromCache(string hash, Action<float> progress = null, CancellationToken token = default) {
			var path = WorldCache.GetWorldFromCache(hash);
			if (!string.IsNullOrEmpty(path))
				return await LoadWorldFromPath(path, progress, token);
			Logger.LogError($"World with hash {hash} not found in cache.");
			return null;
		}

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<AssetBundleWorld> LoadWorldFromPath(string path, Action<float> progress = null, CancellationToken token = default) {
			var existingWorld = GetWorld(AssetBundleWorld.ParseId(path));
			if (existingWorld != null) {
				Logger.LogWarning($"World {path} is already loaded.");
				return existingWorld as AssetBundleWorld;
			}

			var world = await AssetBundleWorld.Load(path, progress, token);

			if (world == null) {
				Logger.LogError($"Failed to load world from path: {path}");
				return null;
			}

			world.Manager = this;
			Worlds.Add(world);
			OnWorldAdded.Invoke(world);
			WorldSystem.CoreAPI.EventAPI.Emit("world_added", world);
			return world;
		}

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<AssetWorld> LoadWorldFromAssets(string ns, string path, Action<float> progress = null, CancellationToken token = default) {
			var existingWorld = GetWorld(AssetWorld.ParseId(ns, path));
			if (existingWorld != null) {
				Logger.LogWarning($"World {ns}:{path} is already loaded.");
				return existingWorld as AssetWorld;
			}

			var world = await AssetWorld.Load(ns, path, progress, token);

			if (world == null) {
				Logger.LogError($"Failed to load world from assets: {ns}:{path}");
				return null;
			}

			world.Manager = this;
			Worlds.Add(world);
			OnWorldAdded.Invoke(world);
			WorldSystem.CoreAPI.EventAPI.Emit("world_added", world);
			return world;
		}

		[NoxPublic(NoxAccess.Method)]
		public BaseWorld GetCurrent() {
			var currentScene = SceneManager.GetActiveScene();
			if (!currentScene.IsValid()) return null;
			return (from world in Worlds
				let scenes = world.GetUnityScenes()
				where scenes.Any(scene => scene.name == currentScene.name)
				select world).FirstOrDefault();
		}

		[NoxPublic(NoxAccess.Method)]
		public bool SetCurrent(string id) {
			var world = GetWorld(id);
			if (world == null) {
				Logger.LogError($"World with id {id} not found.");
				return false;
			}

			var old = GetCurrent();
			if (old == world) {
				Logger.LogDebug($"World {id} is already the current world.");
				return true;
			}

			var canReplace = true;
			WorldSystem.CoreAPI.EventAPI.Emit("world_request_change", world, new Action<object[]>(OnRequest));
			if (!canReplace) {
				Logger.LogDebug($"Canceling world change to {id} due to request.");
				return false;
			}

			old?.MakeNotCurrent(world);
			world.MakeCurrent(old);

			WorldSystem.CoreAPI.EventAPI.Emit("world_changed", world);
			Logger.Log($"Current world set to: {world.Id}");
			return true;

			void OnRequest(object[] args) {
				if (args.Length > 0 && args[0] is false)
					canReplace = false;
			}
		}
	}
}