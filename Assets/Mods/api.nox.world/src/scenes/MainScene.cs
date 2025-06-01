using Nox.CCK.Worlds;
using UnityEngine.SceneManagement;
using Nox.Worlds;
using Nox.Worlds.Components;

namespace api.nox.world {
	public class MainScene : BaseScene<MainDescriptor>, IMainScene {
		public MainScene(BaseWorld world, Scene scene, MainDescriptor descriptor, WorldHidden hidden) : base(world, scene, descriptor, hidden) { }

		public override string ToString()
			=> $"{GetType().Name}[Scene={Scene.name}, World={World}, Visible={Visible}]";
	}
}