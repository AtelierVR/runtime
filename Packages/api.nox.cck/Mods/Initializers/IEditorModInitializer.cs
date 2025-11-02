using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;

namespace Nox.CCK.Mods.Initializers {
	/// <summary>
	/// Interface for editor mod initializers, defining editor-specific lifecycle methods.
	/// </summary>
	public interface IEditorModInitializer : IModInitializer {
		/// <summary>
		/// Called when the mod is being initialized in the editor.
		/// </summary>
		/// <param name="api">The editor core API.</param>
		public void OnInitializeEditor(EditorModCoreAPI api) { }

		/// <summary>
		/// Called asynchronously when the mod is being initialized in the editor.
		/// </summary>
		/// <param name="api">The editor core API.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		public UniTask OnInitializeEditorAsync(EditorModCoreAPI api)
			=> UniTask.CompletedTask;

		/// <summary>
		/// Called after the mod has been initialized in the editor.
		/// </summary>
		public void OnPostInitializeEditor() { }

		/// <summary>
		/// Called asynchronously after the mod has been initialized in the editor.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		public UniTask OnPostInitializeEditorAsync()
			=> UniTask.CompletedTask;

		/// <summary>
		/// Called every frame to update the mod in the editor.
		/// </summary>
		public void OnUpdateEditor()      { }
		
		/// <summary>
		/// Called every frame after all Update methods have been called in the editor.
		/// </summary>
		public void OnLateUpdateEditor()  { }
		
		/// <summary>
		/// Called at a fixed time interval for physics updates in the editor.
		/// </summary>
		public void OnFixedUpdateEditor() { }

		/// <summary>
		/// Called before the mod is disposed in the editor.
		/// </summary>
		public void OnPreDisposeEditor() { }

		/// <summary>
		/// Called asynchronously before the mod is disposed in the editor.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		public UniTask OnPreDisposeEditorAsync()
			=> UniTask.CompletedTask;

		/// <summary>
		/// Called when the mod is being disposed in the editor.
		/// </summary>
		public void OnDisposeEditor() { }

		/// <summary>
		/// Called asynchronously when the mod is being disposed in the editor.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		public UniTask OnDisposeEditorAsync()
			=> UniTask.CompletedTask;
	}
}