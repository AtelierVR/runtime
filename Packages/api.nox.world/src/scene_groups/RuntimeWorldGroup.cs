using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.CCK.Worlds;
using UnityEngine.SceneManagement;
using Nox.Worlds;

namespace api.nox.world {
	public abstract class RuntimeWorldGroup : IRuntimeWorld, INoxObject {
		internal string                    Id;
		internal int                       Active = 0;
		internal MainRuntimeWorldInstance  MainInstance;
		internal SubRuntimeWorldInstance[] SubInstances;
		internal SceneGroupManager         GroupManager;
		internal IWorldIdentifier          WorldIdentifier;

		internal Scene[] GetUnityScenes()
			=> GetScenes()
				.Select(e => e.GetScene())
				.Where(s => s.IsValid())
				.ToArray();

		public IWorldIdentifier GetIdentifier()
			=> WorldIdentifier;

		public void SetIdentifier(IWorldIdentifier identifier)
			=> WorldIdentifier = identifier;

		[NoxPublic(NoxAccess.Method)]
		public IBaseRuntimeWorldInstance<IBaseWorldDescriptor>[] GetScenes() {
			var scenes = new List<IBaseRuntimeWorldInstance<IBaseWorldDescriptor>> { MainInstance };
			scenes.AddRange(SubInstances);
			return scenes.ToArray();
		}

		[NoxPublic(NoxAccess.Method)]
		public IBaseRuntimeWorldInstance<T> GetScene<T>(int index) where T : IBaseWorldDescriptor
			=> index switch {
				0 when typeof(MainWorldDescriptor) == typeof(T) => MainInstance as IBaseRuntimeWorldInstance<T>,
				_ when typeof(SubWorldDescriptor)  == typeof(T) => GetSubScene(index - 1) as IBaseRuntimeWorldInstance<T>,
				_                                               => null
			};

		[NoxPublic(NoxAccess.Method)]
		public IBaseRuntimeWorldInstance<IBaseWorldDescriptor> GetScene(int index)
			=> index == 0 ? GetMainScene() : GetSubScene(index);

		[NoxPublic(NoxAccess.Method)]
		public IMainRuntimeWorldInstance GetMainScene()
			=> MainInstance;

		[NoxPublic(NoxAccess.Method)]
		public ISubRuntimeWorldInstance GetSubScene(int index) {
			if (index < 0 || index >= SubInstances.Length) return null;
			return SubInstances[index];
		}

		[NoxPublic(NoxAccess.Method)]
		public int GetSceneCount()
			=> SubInstances.Length;

		[NoxPublic(NoxAccess.Method)]
		public void SetCurrent()
			=> GroupManager.SetCurrent(Id);

		[NoxPublic(NoxAccess.Method)]
		public bool IsCurrent()
			=> Main.Instance.GetCurrent() == this;

		[NoxPublic(NoxAccess.Method)]
		public virtual async UniTask Dispose() {
			for (var i = 0; i < SubInstances.Length; i++) {
				if (SubInstances[i] == null) continue;
				await UniTask.Yield();
				SubInstances[i].Dispose();
				SubInstances[i] = null;
			}

			await UniTask.Yield();

			if (MainInstance != null) {
				MainInstance.Dispose();
				MainInstance = null;
			}
		}

		internal void OnSelect(RuntimeWorldGroup oldRuntimeWorldGroup) {
			Logger.LogDebug($"OnSelect: {Id}");
			var activeScene = GetScene(Active) ?? GetMainScene();
			foreach (var scene in GetScenes()) {
				if (scene == activeScene) {
					Logger.LogDebug($"Showing the active scene {scene} in world {Id}");
					SceneManager.SetActiveScene(scene.GetScene());
				}

				foreach (var id in scene.GetInstanceIds()) {
					var visible = scene.IsVisibleInstance(id);
					Logger.LogDebug($"{(visible ? "Hiding" : "Showing")} the scene {scene} in world {Id}");
					scene.SetVisibleInstance(id, visible, true);
				}
			}
		}

		internal void OnDeselect(RuntimeWorldGroup newRuntimeWorldGroup) {
			Logger.LogDebug($"OnDeselect: {Id}");
			foreach (var scene in GetScenes())
			foreach (var id in scene.GetInstanceIds())
				scene.SetVisibleInstance(id, false, false);
		}

		public override string ToString()
			=> $"{GetType().Name}[Id={Id} Active={Active}]";
	}
}