using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.CCK.Worlds;
using UnityEngine.SceneManagement;
using Nox.Worlds;

namespace api.nox.world {
	public abstract class BaseWorld : IWorld, INoxObject {
		internal string       Id;
		internal int          Active = 0;
		internal MainScene    MainScene;
		internal SubScene[]   SubScenes;
		internal WorldManager Manager;

		internal Scene[] GetUnityScenes()
			=> GetScenes()
				.Select(e => e.GetScene())
				.Where(s => s.IsValid())
				.ToArray();

		[NoxPublic(NoxAccess.Method)]
		public IScene<BaseDescriptor>[] GetScenes() {
			var scenes = new List<IScene<BaseDescriptor>> { MainScene };
			scenes.AddRange(
				from t in SubScenes
				where t != null
				select t as IScene<BaseDescriptor>
			);
			return scenes.ToArray();
		}

		[NoxPublic(NoxAccess.Method)]
		public IScene<T> GetScene<T>(int index) where T : BaseDescriptor
			=> index switch {
				0 when typeof(MainDescriptor) == typeof(T) => MainScene as IScene<T>,
				_ when typeof(SubDescriptor)  == typeof(T) => GetSubScene(index - 1) as IScene<T>,
				_                                          => null
			};

		[NoxPublic(NoxAccess.Method)]
		public IScene<BaseDescriptor> GetScene(int index)
			=> index == 0 ? GetMainScene() : GetSubScene(index);

		[NoxPublic(NoxAccess.Method)]
		public IMainScene GetMainScene()
			=> MainScene;

		[NoxPublic(NoxAccess.Method)]
		public ISubScene GetSubScene(int index) {
			if (index < 0 || index >= SubScenes.Length) return null;
			return SubScenes[index];
		}

		[NoxPublic(NoxAccess.Method)]
		public int GetSceneCount()
			=> SubScenes.Length;

		[NoxPublic(NoxAccess.Method)]
		public void SetCurrent()
			=> Manager.SetCurrent(Id);

		[NoxPublic(NoxAccess.Method)]
		public bool IsCurrent()
			=> WorldSystem.Instance.GetCurrent() == this;

		[NoxPublic(NoxAccess.Method)]
		public virtual async UniTask Dispose() {
			for (var i = 0; i < SubScenes.Length; i++) {
				if (SubScenes[i] == null) continue;
				await UniTask.Yield();
				SubScenes[i].Dispose();
				SubScenes[i] = null;
			}

			await UniTask.Yield();

			if (MainScene != null) {
				MainScene.Dispose();
				MainScene = null;
			}
		}

		internal void OnSelect(BaseWorld oldWorld) {
			Logger.LogDebug($"OnSelect: {Id}");
			var activeScene = GetScene(Active) ?? GetMainScene();
			foreach (var scene in GetScenes()) {
				if (scene == activeScene) {
					Logger.LogDebug($"Showing the active scene {scene} in world {Id}");
					scene.GetWorldHidden().Set(true);
					scene.SetVisible(true);
					SceneManager.SetActiveScene(scene.GetScene());
				} else {
					var visible = scene.IsVisible();
					Logger.LogDebug($"{(visible ? "Hiding" : "Showing")} the scene {scene} in world {Id}");
					scene.GetWorldHidden().Set(visible);
					scene.SetVisible(visible);
				}
			}
		}

		internal void OnDeselect(BaseWorld newWorld) {
			Logger.LogDebug($"OnDeselect: {Id}");
			foreach (var scene in GetScenes())
				scene.GetWorldHidden().Set(false);
		}

		public override string ToString()
			=> $"{GetType().Name}[Id={Id} Active={Active}]";
	}
}