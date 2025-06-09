using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Nox.Worlds {
	/// <summary>
	/// Interface for the World API, providing methods to load worlds from various sources.
	/// </summary>
	public interface IWorldAPI {
		/// <summary>
		/// Loads a world from the given path in the filesystem.
		/// Return null if the world is not found at the specified path.
		/// </summary>
		/// <param name="path">Path to the world file.</param>
		/// <param name="progress">Progress callback to report loading progress.</param>
		/// <param name="token">Cancellation token to cancel the loading operation.</param>
		/// <returns>Returns a <see cref="ILoadedWorld"/> instance representing the loaded world.</returns>
		public UniTask<ILoadedWorld> LoadWorldFromPath(string path, Action<float> progress = null, CancellationToken token = default);

		/// <summary>
		/// Loads a world from the given path in the assets.
		/// Return null if the world is not found in the assets.
		/// </summary>
		/// <param name="ns">Namespace of the mod that contains the world.</param>
		/// <param name="path">Path to the world file in the assets.</param>
		/// <param name="progress">Progress callback to report loading progress.</param>
		/// <param name="token">Cancellation token to cancel the loading operation.</param>
		/// <returns>Returns a <see cref="ILoadedWorld"/> instance representing the loaded world.</returns>
		public UniTask<ILoadedWorld> LoadWorldFromAssets(string ns, string path, Action<float> progress = null, CancellationToken token = default);

		/// <summary>
		/// Loads a world from the cache using its hash.
		/// If the world is not found in the cache, it will return null.
		/// </summary>
		/// <param name="hash">Hash of the world to load.</param>
		/// <param name="progress">Progress callback to report loading progress.</param>
		/// <param name="token">Cancellation token to cancel the loading operation.</param>
		/// <returns>Returns a <see cref="ILoadedWorld"/> instance representing the loaded world, or null if not found.</returns>
		public UniTask<ILoadedWorld> LoadWorldFromCache(string hash, Action<float> progress = null, CancellationToken token = default);

		/// <summary>
		/// Gets the currently active world.
		/// </summary>
		/// <returns></returns>
		public ILoadedWorld GetCurrent();

		/// <summary>
		/// Sets the current world by its ID.
		/// If the world with the given ID does not exist or cannot be set as current,
		/// If the world is successfully or already set as current, it will return true.
		/// </summary>
		/// <param name="id">Identifier of the world to set as current.</param>
		/// <returns>Returns true if the world was successfully set as current, false otherwise.</returns>
		public bool SetCurrent(string id);
		
		public UniTask<IWorld> Fetch(string id, string from = null);
	}
}