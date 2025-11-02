using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Metadata;

namespace Nox.CCK.Mods.Mods {
	/// <summary>
	/// API for managing mods at runtime.
	/// </summary>
	public interface IModAPI {
		/// <summary>
		/// Gets a mod by its identifier.
		/// </summary>
		/// <param name="id">The mod identifier.</param>
		/// <returns>The mod instance, or null if not found.</returns>
		public IMod GetMod(string id);

		/// <summary>
		/// Gets all loaded mods.
		/// </summary>
		/// <returns>An array of all mod instances.</returns>
		public IMod[] GetMods();

		/// <summary>
		/// Loads a mod by its identifier asynchronously.
		/// </summary>
		/// <param name="id">The mod identifier.</param>
		/// <returns>A task representing the asynchronous operation with the loaded mod instance.</returns>
		public UniTask<IMod> LoadMod(string id);

		/// <summary>
		/// Unloads a mod by its identifier asynchronously.
		/// </summary>
		/// <param name="id">The mod identifier.</param>
		/// <returns>A task representing the asynchronous operation, with a boolean indicating success.</returns>
		public UniTask<bool> UnloadMod(string id);

		/// <summary>
		/// Reloads a mod by its identifier asynchronously.
		/// </summary>
		/// <param name="id">The mod identifier.</param>
		/// <returns>A task representing the asynchronous operation, with a boolean indicating success.</returns>
		public UniTask<bool> ReloadMod(string id);

		/// <summary>
		/// Gets the metadata of a mod by its identifier.
		/// </summary>
		/// <param name="id">The mod identifier.</param>
		/// <returns>The mod metadata.</returns>
		public ModMetadata GetMetadata(string id);

		/// <summary>
		/// Gets the current mod instance (the mod calling this method).
		/// </summary>
		/// <returns>The current mod instance.</returns>
		public IMod GetSelf();
	}
}