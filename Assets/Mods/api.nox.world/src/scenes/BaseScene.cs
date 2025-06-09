using Nox.CCK.Utils;
using Nox.CCK.Worlds;
using UnityEngine.SceneManagement;
using Nox.Worlds;
using Nox.Worlds.Components;

namespace api.nox.world {
	public class BaseScene<T> : IScene<T>, INoxObject where T : BaseDescriptor {
		internal BaseLoadedWorld   LoadedWorld;
		internal Scene       Scene;
		internal bool        Visible;
		internal T           Descriptor;
		internal WorldHidden Hidden;

		public BaseScene(BaseLoadedWorld loadedWorld, Scene scene, T descriptor, WorldHidden hidden) {
			LoadedWorld      = loadedWorld;
			Scene      = scene;
			Descriptor = descriptor;
			Hidden     = hidden;
			Visible    = !Hidden.IsHidden();
		}

		[NoxPublic(NoxAccess.Method)]
		public Scene GetScene()
			=> Scene;

		[NoxPublic(NoxAccess.Method)]
		public T GetDescriptor()
			=> Descriptor;

		[NoxPublic(NoxAccess.Method)]
		public WorldHidden GetWorldHidden()
			=> Hidden;

		[NoxPublic(NoxAccess.Method)]
		public void SetVisible(bool active) {
			if (LoadedWorld.IsCurrent() && active && Hidden.IsHidden())
				Hidden.Set(true);
			if (LoadedWorld.IsCurrent() && !active && !Hidden.IsHidden())
				Hidden.Set(false);
			Visible = active;
		}

		[NoxPublic(NoxAccess.Method)]
		public bool IsVisible() {
			if (LoadedWorld.IsCurrent())
				return !Hidden.IsHidden();
			return Visible;
		}

		[NoxPublic(NoxAccess.Method)]
		public void Dispose() {
			SetVisible(false);
			Scene      = default;
			Descriptor = null;
			Hidden     = null;
			LoadedWorld      = null;
		}

		public override string ToString()
			=> $"{GetType().Name}<{typeof(T).Name}>[Scene={Scene.name}, World={LoadedWorld}, Visible={Visible}]";
	}
}