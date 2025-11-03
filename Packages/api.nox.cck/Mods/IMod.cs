using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Assets;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.Metadata;

namespace Nox.CCK.Mods {
	/// <summary>
	/// Represents a mod instance with its lifecycle and data management capabilities.
	/// </summary>
	public interface IMod {
		/// <summary>
		/// Gets the metadata of the mod.
		/// </summary>
		/// <returns>The mod metadata.</returns>
		public ModMetadata GetMetadata();

		/// <summary>
		/// Gets a data value associated with the specified key.
		/// </summary>
		/// <typeparam name="T">The type of the data value.</typeparam>
		/// <param name="key">The key of the data to retrieve.</param>
		/// <param name="defaultValue">The default value to return if the key is not found.</param>
		/// <returns>The data value, or the default value if not found.</returns>
		public T GetData<T>(string key, T defaultValue = default);

		/// <summary>
		/// Sets a data value for the specified key.
		/// </summary>
		/// <typeparam name="T">The type of the data value.</typeparam>
		/// <param name="key">The key of the data to set.</param>
		/// <param name="value">The value to set.</param>
		/// <returns>True if the data was successfully set; otherwise, false.</returns>
		public bool SetData<T>(string key, T value);

		/// <summary>
		/// Checks if a data value exists for the specified key.
		/// </summary>
		/// <typeparam name="T">The type of the data value.</typeparam>
		/// <param name="key">The key to check.</param>
		/// <returns>True if the data exists; otherwise, false.</returns>
		public bool HasData<T>(string key);

		/// <summary>
		/// Gets all data values stored in the mod.
		/// </summary>
		/// <returns>A dictionary containing all data key-value pairs.</returns>
		public Dictionary<string, object> GetDatas();

		/// <summary>
		/// Checks if the mod is currently loaded.
		/// </summary>
		/// <returns>True if the mod is loaded; otherwise, false.</returns>
		public bool IsLoaded();

		/// <summary>
		/// Loads the mod asynchronously.
		/// </summary>
		/// <returns>A task representing the asynchronous operation, with a boolean indicating success.</returns>
		public UniTask<bool> Load();

		/// <summary>
		/// Unloads the mod asynchronously.
		/// </summary>
		/// <returns>A task representing the asynchronous operation, with a boolean indicating success.</returns>
		public UniTask<bool> Unload();

		/// <summary>
		/// Gets the profiling data for the mod.
		/// </summary>
		/// <returns>An array of profiling profiles.</returns>
		public IEnumerable<Profile> GetProfiler();

		/// <summary>
		/// Gets the AppDomain in which the mod is running.
		/// </summary>
		/// <returns>The mod's AppDomain.</returns>
		public AppDomain GetAppDomain();

		/// <summary>
		/// Gets an instance of the specified type from the mod.
		/// </summary>
		/// <typeparam name="T">The type of instance to retrieve.</typeparam>
		/// <returns>An instance of the specified type.</returns>
		public T GetInstance<T>();

		/// <summary>
		/// Gets all instances of the specified type from the mod.
		/// </summary>
		/// <typeparam name="T">The type of instances to retrieve.</typeparam>
		/// <returns>An array of instances of the specified type.</returns>
		public T[] GetInstances<T>();
	}
}