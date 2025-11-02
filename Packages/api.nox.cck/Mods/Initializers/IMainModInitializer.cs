using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;

namespace Nox.CCK.Mods.Initializers
{
    /// <summary>
    /// Interface for main mod initializers, defining main application lifecycle methods.
    /// </summary>
    public interface IMainModInitializer : IModInitializer
    {
        /// <summary>
        /// Called when the mod is being initialized in the main application.
        /// </summary>
        /// <param name="api">The main core API.</param>
        public void OnInitializeMain(MainModCoreAPI api) { }
        
        /// <summary>
        /// Called asynchronously when the mod is being initialized in the main application.
        /// </summary>
        /// <param name="api">The main core API.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public UniTask OnInitializeMainAsync(MainModCoreAPI api) => UniTask.CompletedTask;
        
        /// <summary>
        /// Called after the mod has been initialized in the main application.
        /// </summary>
        public void OnPostInitializeMain() { }
        
        /// <summary>
        /// Called asynchronously after the mod has been initialized in the main application.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public UniTask OnPostInitializeMainAsync() => UniTask.CompletedTask;
        
        /// <summary>
        /// Called every frame to update the mod in the main application.
        /// </summary>
        public void OnUpdateMain() { }
        
        /// <summary>
        /// Called every frame after all Update methods have been called in the main application.
        /// </summary>
        public void OnLateUpdateMain() { }
        
        /// <summary>
        /// Called at a fixed time interval for physics updates in the main application.
        /// </summary>
        public void OnFixedUpdateMain() { }

        /// <summary>
        /// Called before the mod is disposed in the main application.
        /// </summary>
        public void OnPreDisposeMain() { }
        
        /// <summary>
        /// Called asynchronously before the mod is disposed in the main application.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public UniTask OnPreDisposeMainAsync() => UniTask.CompletedTask;
        
        /// <summary>
        /// Called when the mod is being disposed in the main application.
        /// </summary>
        public void OnDisposeMain() { }
        
        /// <summary>
        /// Called asynchronously when the mod is being disposed in the main application.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public UniTask OnDisposeMainAsync() => UniTask.CompletedTask;
    }
}