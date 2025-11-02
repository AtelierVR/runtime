using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;

namespace Nox.CCK.Mods.Initializers {
	/// <summary>
	/// Interface for client-side mod initializers, defining client-specific lifecycle methods.
	/// </summary>
	public interface IClientModInitializer : IModInitializer {
		/// <summary>
		/// Called when the mod is being initialized on the client.
		/// </summary>
		/// <param name="api">The client core API.</param>
		public void OnInitializeClient(ClientModCoreAPI api) { }

		/// <summary>
		/// Called asynchronously when the mod is being initialized on the client.
		/// </summary>
		/// <param name="api">The client core API.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		public UniTask OnInitializeClientAsync(ClientModCoreAPI api)
			=> UniTask.CompletedTask;

		/// <summary>
		/// Called after the mod has been initialized on the client.
		/// </summary>
		public void OnPostInitializeClient() { }

		/// <summary>
		/// Called asynchronously after the mod has been initialized on the client.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		public UniTask OnPostInitializeClientAsync()
			=> UniTask.CompletedTask;

		/// <summary>
		/// Called every frame to update the mod on the client.
		/// </summary>
		public void OnUpdateClient()      { }
		
		/// <summary>
		/// Called every frame after all Update methods have been called on the client.
		/// </summary>
		public void OnLateUpdateClient()  { }
		
		/// <summary>
		/// Called at a fixed time interval for physics updates on the client.
		/// </summary>
		public void OnFixedUpdateClient() { }

		/// <summary>
		/// Called before the mod is disposed on the client.
		/// </summary>
		public void OnPreDisposeClient() { }

		/// <summary>
		/// Called asynchronously before the mod is disposed on the client.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		public UniTask OnPreDisposeClientAsync()
			=> UniTask.CompletedTask;

		/// <summary>
		/// Called when the mod is being disposed on the client.
		/// </summary>
		public void OnDisposeClient() { }

		/// <summary>
		/// Called asynchronously when the mod is being disposed on the client.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		public UniTask OnDisposeClientAsync()
			=> UniTask.CompletedTask;
	}
}