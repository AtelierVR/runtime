using Nox.CCK.Worlds;
using UnityEngine.SceneManagement;
using Nox.Worlds.Components;

namespace Nox.Worlds {
	public interface IScene<out T> where T : BaseDescriptor {
		/// <summary>
		/// Returns the Unity scene associated with this IScene.
		/// </summary>
		/// <returns></returns>
		public Scene GetScene();

		/// <summary>
		/// Returns the descriptor associated with this scene.
		/// </summary>
		/// <returns><see cref="BaseDescriptor"/></returns>
		public T GetDescriptor();

		/// <summary>
		/// Returns the WorldHidden associated with this scene.
		/// </summary>
		/// <returns></returns>
		public WorldHidden GetWorldHidden();

		/// <summary>
		/// Hide or show the scene in the world.
		/// Make immediately the setting when the world is current.
		/// </summary>
		/// <param name="active"></param>
		public void SetVisible(bool active);

		/// <summary>
		/// Checks if the scene is active in the world.
		/// </summary>
		/// <returns></returns>
		public bool IsVisible();
	}
}