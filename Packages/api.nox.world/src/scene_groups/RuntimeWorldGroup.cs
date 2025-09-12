using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.CCK.Worlds;
using UnityEngine.SceneManagement;
using Nox.Worlds;

namespace api.nox.world {
	public abstract class RuntimeWorldGroup : IRuntimeWorld, INoxObject {
		internal string                 Id;
		internal int                    Active = 0;
		internal RuntimeWorldInstance[] Instances;
		internal SceneGroupManager      GroupManager;
		internal IWorldIdentifier       WorldIdentifier;

		internal Scene[] GetUnityScenes()
			=> GetInstances()
				.Select(e => e.GetScene())
				.Where(s => s.IsValid())
				.ToArray();

		public IWorldIdentifier GetIdentifier()
			=> WorldIdentifier;

		public void SetIdentifier(IWorldIdentifier identifier)
			=> WorldIdentifier = identifier;

		[NoxPublic(NoxAccess.Method)]
		public IRuntimeWorldInstance[] GetInstances()
			=> Instances.Cast<IRuntimeWorldInstance>().ToArray();

		[NoxPublic(NoxAccess.Method)]
		public IRuntimeWorldInstance GetInstance(int index)
			=> index >= 0 && index < Instances.Length
				? Instances[index]
				: null;

		public int GetInstanceCount()
			=> Instances.Length;

		[NoxPublic(NoxAccess.Method)]
		public void SetCurrent()
			=> GroupManager.SetCurrent(Id);

		[NoxPublic(NoxAccess.Method)]
		public bool IsCurrent()
			=> Main.Instance.GetCurrent() == this;

		[NoxPublic(NoxAccess.Method)]
		public virtual async UniTask Dispose() {
			await UniTask.Yield();
			foreach (var t in Instances)
				t?.Dispose();
			Instances = Array.Empty<RuntimeWorldInstance>();
		}

		internal void OnSelect(RuntimeWorldGroup old) {
			Logger.LogDebug($"OnSelect: {Id}");
			var active = GetInstance(Active) ?? GetInstances()[0];
			foreach (var scene in GetInstances()) {
				if (scene == active) {
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

		internal void OnDeselect(RuntimeWorldGroup @rew) {
			Logger.LogDebug($"OnDeselect: {Id}");
			foreach (var scene in GetInstances())
			foreach (var id in scene.GetInstanceIds())
				scene.SetVisibleInstance(id, false, false);
		}

		public override string ToString()
			=> $"{GetType().Name}[Id={Id} Active={Active}]";
	}
}