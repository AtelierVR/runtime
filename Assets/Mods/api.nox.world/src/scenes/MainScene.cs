using Nox.CCK.Worlds;
using UnityEngine.SceneManagement;
using Nox.Worlds;
using Nox.Worlds.Components;

namespace api.nox.world {
	public class MainScene : BaseScene<MainDescriptor>, IMainScene {
		public MainScene(BaseLoadedWorld loadedWorld, Scene scene, MainDescriptor descriptor, WorldHidden hidden) : base(loadedWorld, scene, descriptor, hidden) { }

		public override string ToString()
			=> $"{GetType().Name}[Scene={Scene.name}, World={LoadedWorld}, Visible={Visible}]";
	}
}