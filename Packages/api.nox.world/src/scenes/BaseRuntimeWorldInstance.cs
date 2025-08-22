using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.CCK.Worlds;
using Nox.Worlds;
using UnityEngine.SceneManagement;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.world {
	public class BaseRuntimeWorldInstance<T> : IBaseRuntimeWorldInstance<T>, INoxObject where T : IBaseWorldDescriptor {
		internal RuntimeWorldGroup      RuntimeWorldGroup;
		internal Scene                  Scene;
		internal GameObject             Anchor;
		internal GameObject             Prefab;
		internal List<InstanceScene<T>> Instances = new();

		public BaseRuntimeWorldInstance(RuntimeWorldGroup init, Scene scene, GameObject prefab) {
			RuntimeWorldGroup = init;
			Scene             = scene;
			Prefab            = prefab;
		}

		[NoxPublic(NoxAccess.Method)]
		public Scene GetScene()
			=> Scene;

		public async UniTask<int> MakeInstance() {
			if (!Prefab)
				return -1;

			Prefab.SetActive(false);
			var container = await Object.InstantiateAsync(Prefab);
			if (container.Length != 1) {
				foreach (var go in container)
					Object.Destroy(go);
				return -1;
			}

			SceneManager.MoveGameObjectToScene(container[0], Scene);

			var instance = new InstanceScene<T> {
				Container  = container[0],
				Descriptor = WorldDescriptorExtension.TryGetDescriptor(container[0], out T desc) ? desc : default,
			};

			container[0].name = $"{GetType().Name}_{instance.GetId()}]";

			if (instance.Descriptor == null) {
				Object.Destroy(container[0]);
				return -1;
			}

			Instances.Add(instance);
			return instance.GetId();
		}

		private InstanceScene<T> GetInstance(int id)
			=> Instances.FirstOrDefault(x => x.GetId() == id);

		public T GetInstanceDescriptor(int id) {
			var instance = GetInstance(id);
			return instance == null 
				? default 
				: instance.Descriptor;
		}

		public void SetVisibleInstance(int id, bool active, bool save) {
			var instance = GetInstance(id);
			if (instance == null) {
				Logger.LogWarning($"BaseScene<{typeof(T).Name}>: Instance with ID {id} not found.");
				return;
			}

			instance.Container.SetActive(active);
			instance.Visible = active;
		}

		public int[] GetInstanceIds()
			=> Instances.Select(x => x.GetId()).ToArray();

		public void RemoveInstance(int id) {
			var instance = GetInstance(id);
			if (instance == null) {
				Logger.LogWarning($"BaseScene<{typeof(T).Name}>: Instance with ID {id} not found.");
				return;
			}

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
			Scene             = default;
			RuntimeWorldGroup = null;
		}

		public override string ToString()
			=> $"{GetType().Name}<{typeof(T).Name}>[Scene={Scene.name}, World={RuntimeWorldGroup}]";
	}
}