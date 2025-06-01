using Nox.CCK.Worlds;
using UnityEngine.SceneManagement;
using Nox.Worlds;
using Nox.Worlds.Components;

namespace api.nox.world {
	public class SubScene : BaseScene<SubDescriptor>, ISubScene {
		public SubScene(BaseWorld world, Scene scene, SubDescriptor descriptor, WorldHidden hidden) : base(world, scene, descriptor, hidden) { }

		public override string ToString()
			=> $"{GetType().Name}[Scene={Scene.name}, World={World}, Visible={Visible}]";
	}
}