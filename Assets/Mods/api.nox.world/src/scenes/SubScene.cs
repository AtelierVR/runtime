using Nox.CCK.Worlds;
using UnityEngine.SceneManagement;
using Nox.Worlds;
using UnityEngine;

namespace api.nox.world {
	public class SubScene : BaseScene<SubDescriptor>, ISubSceneDescription {
		public SubScene(SceneGroup init, Scene scene, GameObject prefab) : base(init, scene, prefab) { }

		public override string ToString()
			=> $"{GetType().Name}[Scene={Scene.name}, World={SceneGroup}]";
	}
}