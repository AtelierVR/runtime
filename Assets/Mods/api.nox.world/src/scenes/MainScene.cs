using Nox.CCK.Worlds;
using UnityEngine.SceneManagement;
using Nox.Worlds;
using Nox.Worlds.Components;

namespace api.nox.world {
	public class MainBaseScene : BaseScene<MainDescriptor>, IMainSceneDescription {
		public MainBaseScene(SceneGroup init, Scene scene, MainDescriptor descriptor, WorldHidden hidden) : base(init, scene, descriptor, hidden) { }

		public override string ToString()
			=> $"{GetType().Name}[Scene={Scene.name}, World={SceneGroup}, Visible={Visible}]";
	}
}