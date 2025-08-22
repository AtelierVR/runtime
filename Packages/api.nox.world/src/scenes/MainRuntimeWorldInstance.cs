using Nox.CCK.Worlds;
using Nox.Worlds;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace api.nox.world {
	public class MainRuntimeWorldInstance : BaseRuntimeWorldInstance<IMainWorldDescriptor>, IMainRuntimeWorldInstance {
		public MainRuntimeWorldInstance(RuntimeWorldGroup init, Scene scene, GameObject prefab) : base(init, scene, prefab) { }

		public override string ToString()
			=> $"{GetType().Name}[Scene={Scene.name}, World={RuntimeWorldGroup}]";
	}
}