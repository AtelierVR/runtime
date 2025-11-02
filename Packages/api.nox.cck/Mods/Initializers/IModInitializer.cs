using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Utils;

namespace Nox.CCK.Mods.Initializers {
	/// <summary>
	/// Base interface for mod initializers, defining the lifecycle methods for mods.
	/// </summary>
	public interface IModInitializer : INoxObject {
		/// <summary>
		/// Called when the mod is being initialized.
		/// </summary>
		/// <param name="api">The core API for the mod.</param>
		public void OnInitialize(IModCoreAPI api) { }

		/// <summary>
		/// Called asynchronously when the mod is being initialized.
		/// </summary>
		/// <param name="api">The core API for the mod.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		public UniTask OnInitializeAsync(IModCoreAPI api)
			=> UniTask.CompletedTask;

		/// <summary>
		/// Called after the mod has been initialized.
		/// </summary>
		public void OnPostInitialize() { }

		/// <summary>
		/// Called asynchronously after the mod has been initialized.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		public UniTask OnPostInitializeAsync()
			=> UniTask.CompletedTask;

		/// <summary>
		/// Called every frame to update the mod.
		/// </summary>
		public void OnUpdate()      { }
		
		/// <summary>
		/// Called every frame after all Update methods have been called.
		/// </summary>
		public void OnLateUpdate()  { }
		
		/// <summary>
		/// Called at a fixed time interval for physics updates.
		/// </summary>
		public void OnFixedUpdate() { }
		
		/// <summary>
		/// Called before the mod is disposed.
		/// </summary>
		public void OnPreDispose()  { }

		/// <summary>
		/// Called asynchronously before the mod is disposed.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		public UniTask OnPreDisposeAsync()
			=> UniTask.CompletedTask;

		/// <summary>
		/// Called when the mod is being disposed.
		/// </summary>
		public void OnDispose() { }

		/// <summary>
		/// Called asynchronously when the mod is being disposed.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		public UniTask OnDisposeAsync()
			=> UniTask.CompletedTask;
	}
}