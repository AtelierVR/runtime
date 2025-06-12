using Nox.CCK.Worlds;
using UnityEngine.SceneManagement;
using Nox.Worlds;
using UnityEngine;

namespace api.nox.world {
	public class MainScene : BaseScene<MainDescriptor>, IMainSceneDescription {
		public MainScene(SceneGroup init, Scene scene, GameObject prefab) : base(init, scene, prefab) { }

		public override string ToString()
			=> $"{GetType().Name}[Scene={Scene.name}, World={SceneGroup}]";
	}
}