using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Utils;
using Nox.CCK.Worlds;
using UnityEngine.SceneManagement;
using Nox.Worlds;
using UnityEngine;

namespace api.nox.world {
	public class BaseScene<T> : ISceneDescription<T>, INoxObject where T : BaseDescriptor {
		internal SceneGroup             SceneGroup;
		internal Scene                  Scene;
		internal GameObject             Anchor;
		internal GameObject             Prefab;
		internal List<InstanceScene<T>> Instances = new();

		public BaseScene(SceneGroup init, Scene scene, GameObject prefab) {
			SceneGroup = init;
			Scene      = scene;
			Prefab     = prefab;
		}

		[NoxPublic(NoxAccess.Method)]
		public Scene GetScene()
			=> Scene;


		public int MakeInstance() {
			if (!Prefab)
				return -1;

			Prefab.SetActive(false);
			var container = Object.Instantiate(Prefab);
			if (!container)
				return -1;

			SceneManager.MoveGameObjectToScene(container, Scene);

			var instance = new InstanceScene<T> {
				Container  = container,
				Descriptor = BaseDescriptor.TryGetDescriptor(container, out T desc) ? desc : null,
			};
			
			container.name = $"{GetType().Name}_{instance.GetId()}]";

			if (!instance.Descriptor) {
				Object.Destroy(container);
				return -1;
			}

			Instances.Add(instance);
			return instance.GetId();
		}

		private InstanceScene<T> GetInstance(int id)
			=> Instances.FirstOrDefault(x => x.GetId() == id);

		public T GetInstanceDescriptor(int id)
			=> GetInstance(id)?.Descriptor;

		public void SetVisibleInstance(int id, bool active, bool save) {
			var instance = GetInstance(id);
			if (instance == null) return;
			instance.Container.SetActive(active);
			instance.Visible = active;
		}

		public int[] GetInstanceIds()
			=> Instances.Select(x => x.GetId()).ToArray();

		public void RemoveInstance(int id) {
			var instance = GetInstance(id);
			if (instance == null) return;
			Instances.Remove(instance);
			Object.Destroy(instance.Container);
		}

		public bool IsVisibleInstance(int id)
			=> GetInstance(id)?.Container.activeSelf ?? false;

		[NoxPublic(NoxAccess.Method)]
		public void Dispose() {
			foreach (var instance in Instances)
				RemoveInstance(instance.GetId());
			Instances.Clear();
			Scene      = default;
			SceneGroup = null;
		}

		public override string ToString()
			=> $"{GetType().Name}<{typeof(T).Name}>[Scene={Scene.name}, World={SceneGroup}]";
	}
}