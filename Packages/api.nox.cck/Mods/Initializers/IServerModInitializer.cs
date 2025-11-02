using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;

namespace Nox.CCK.Mods.Initializers {
	/// <summary>
	/// Interface for server-side mod initializers, defining server-specific lifecycle methods.
	/// </summary>
	public interface IServerModInitializer : IModInitializer {
		/// <summary>
		/// Called when the mod is being initialized on the server.
		/// </summary>
		/// <param name="api">The server core API.</param>
		public void OnInitializeServer(ServerModCoreAPI api) { }

		/// <summary>
		/// Called asynchronously when the mod is being initialized on the server.
		/// </summary>
		/// <param name="api">The server core API.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		public UniTask OnInitializeServerAsync(ServerModCoreAPI api)
			=> UniTask.CompletedTask;

		/// <summary>
		/// Called after the mod has been initialized on the server.
		/// </summary>
		public void OnPostInitializeServer() { }

		/// <summary>
		/// Called asynchronously after the mod has been initialized on the server.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		public UniTask OnPostInitializeServerAsync()
			=> UniTask.CompletedTask;

		/// <summary>
		/// Called every frame to update the mod on the server.
		/// </summary>
		public void OnUpdateServer()      { }
		
		/// <summary>
		/// Called every frame after all Update methods have been called on the server.
		/// </summary>
		public void OnLateUpdateServer()  { }
		
		/// <summary>
		/// Called at a fixed time interval for physics updates on the server.
		/// </summary>
		public void OnFixedUpdateServer() { }

		/// <summary>
		/// Called before the mod is disposed on the server.
		/// </summary>
		public void OnPreDisposeServer() { }

		/// <summary>
		/// Called asynchronously before the mod is disposed on the server.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		public UniTask OnPreDisposeServerAsync()
			=> UniTask.CompletedTask;

		/// <summary>
		/// Called when the mod is being disposed on the server.
		/// </summary>
		public void OnDisposeServer() { }

		/// <summary>
		/// Called asynchronously when the mod is being disposed on the server.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		public UniTask OnDisposeServerAsync()
			=> UniTask.CompletedTask;
	}
}