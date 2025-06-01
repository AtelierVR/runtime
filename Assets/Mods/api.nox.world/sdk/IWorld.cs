using Cysharp.Threading.Tasks;
using Nox.CCK.Worlds;

namespace Nox.Worlds {
	/// <summary>
	/// Represents the world in which all scenes are loaded.
	/// </summary>
	public interface IWorld {
		/// <summary>
		/// Returns all loaded scenes in the world.
		/// </summary>
		/// <returns></returns>
		public IScene<BaseDescriptor>[] GetScenes();

		/// <summary>
		/// Returns the scene at the given index.
		/// The index 0 is reserved for the main scene.
		/// All other scenes are loaded as sub-scenes or null if not loaded.
		/// </summary>
		/// <param name="index"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public IScene<T> GetScene<T>(int index) where T : BaseDescriptor;

		/// <summary>
		/// Returns the scene at the given index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public IScene<BaseDescriptor> GetScene(int index);

		/// <summary>
		/// Returns the main scene.
		/// </summary>
		/// <returns></returns>
		public IMainScene GetMainScene();

		/// <summary>
		/// Returns the sub-scene at the given index.
		/// Can return null if the sub-scene is not loaded.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public ISubScene GetSubScene(int index);

		/// <summary>
		/// Returns the number of scenes in the world.
		/// </summary>
		/// <returns></returns>
		public int GetSceneCount();

		/// <summary>
		/// Makes the world current.
		/// </summary>
		public void SetCurrent();

		/// <summary>
		/// Checks if the world is current.
		/// </summary>
		/// <returns></returns>
		public bool IsCurrent();

		/// <summary>
		/// Disposes the world and unloads all scenes.
		/// </summary>
		/// <returns></returns>
		public UniTask Dispose();
	}
}