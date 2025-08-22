using Nox.Worlds;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace api.nox.world {
	public class SubRuntimeWorldInstance : BaseRuntimeWorldInstance<ISubWorldDescriptor>, ISubRuntimeWorldInstance {
		public SubRuntimeWorldInstance(RuntimeWorldGroup init, Scene scene, GameObject prefab) : base(init, scene, prefab) { }

		public override string ToString()
			=> $"{GetType().Name}[Scene={Scene.name}, World={RuntimeWorldGroup}]";
	}
}