using Nox.CCK.Worlds;
using UnityEngine.SceneManagement;
using Nox.Worlds;
using Nox.Worlds.Components;

namespace api.nox.world {
	public class SubBaseScene : BaseScene<SubDescriptor>, ISubSceneDescription {
		public SubBaseScene(SceneGroup init, Scene scene, SubDescriptor descriptor, WorldHidden hidden) : base(init, scene, descriptor, hidden) { }

		public override string ToString()
			=> $"{GetType().Name}[Scene={Scene.name}, World={SceneGroup}, Visible={Visible}]";
	}
}