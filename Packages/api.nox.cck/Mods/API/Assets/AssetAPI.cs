using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nox.CCK.Mods.Assets
{
    /// <summary>
    /// API for managing assets and worlds (scenes) from mods, with support for namespaces and overrides.
    /// </summary>
    public interface IAssetAPI
    {
        /// <summary>
        /// Gets all asset names across all namespaces.
        /// </summary>
        /// <returns>An array of key-value pairs containing namespace and asset name.</returns>
        public KeyValuePair<string, string>[] GetAssetNames();
        
        /// <summary>
        /// Gets all asset names in a specific namespace.
        /// </summary>
        /// <param name="ns">The namespace to query.</param>
        /// <returns>An array of key-value pairs containing namespace and asset name.</returns>
        public KeyValuePair<string, string>[] GetAssetNames(string ns);
        
        /// <summary>
        /// Gets all local asset names (from the current mod).
        /// </summary>
        /// <returns>An array of key-value pairs containing namespace and asset name.</returns>
        public KeyValuePair<string, string>[] GetLocalAssetNames();

        /// <summary>
        /// Gets all override asset names in a specific namespace.
        /// </summary>
        /// <param name="ns">The namespace to query.</param>
        /// <returns>An array of key-value pairs containing namespace and asset name.</returns>
        public KeyValuePair<string, string>[] GetOverrideAssetNames(string ns);

        /// <summary>
        /// Checks if an asset exists in the specified namespace.
        /// Priority: override local assets, override assets, local assets.
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="ns">The namespace.</param>
        /// <param name="name">The asset name.</param>
        /// <returns>True if the asset exists; otherwise, false.</returns>
        public bool HasAsset<T>(string ns, string name)
            where T : Object;

        /// <summary>
        /// Gets an asset from the specified namespace.
        /// Priority: override local assets, override assets, local assets.
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="ns">The namespace.</param>
        /// <param name="name">The asset name.</param>
        /// <returns>The asset, or null if not found.</returns>
        public T GetAsset<T>(string ns, string name)
            where T : Object;

        /// <summary>
        /// Checks if an asset exists by name.
        /// Priority: override local assets, override assets, local assets.
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="name">The asset name.</param>
        /// <returns>True if the asset exists; otherwise, false.</returns>
        public bool HasAsset<T>(string name)
            where T : Object;

        /// <summary>
        /// Gets an asset by name.
        /// Priority: override local assets, override assets, local assets.
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="name">The asset name.</param>
        /// <returns>The asset, or null if not found.</returns>
        public T GetAsset<T>(string name)
            where T : Object;

        /// <summary>
        /// Checks if a local asset exists (from the current mod only).
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="name">The asset name.</param>
        /// <returns>True if the local asset exists; otherwise, false.</returns>
        public bool HasLocalAsset<T>(string name) where T : Object;
        
        /// <summary>
        /// Gets a local asset (from the current mod only).
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="name">The asset name.</param>
        /// <returns>The local asset, or null if not found.</returns>
        public T GetLocalAsset<T>(string name) where T : Object;

        /// <summary>
        /// Checks if an override asset exists in the specified namespace.
        /// Priority: override local assets first.
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="ns">The namespace.</param>
        /// <param name="name">The asset name.</param>
        /// <returns>True if the override asset exists; otherwise, false.</returns>
        public bool HasOverrideAsset<T>(string ns, string name) where T : Object;
        
        /// <summary>
        /// Gets an override asset from the specified namespace.
        /// Priority: override local assets first.
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="ns">The namespace.</param>
        /// <param name="name">The asset name.</param>
        /// <returns>The override asset, or null if not found.</returns>
        public T GetOverrideAsset<T>(string ns, string name) where T : Object;

        /// <summary>
        /// Asynchronously checks if an asset exists in the specified namespace.
        /// Priority: override local assets, override assets, local assets.
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="ns">The namespace.</param>
        /// <param name="name">The asset name.</param>
        /// <returns>A task with a boolean indicating if the asset exists.</returns>
        public UniTask<bool> HasAssetAsync<T>(string ns, string name)
            where T : Object;

        /// <summary>
        /// Asynchronously gets an asset from the specified namespace.
        /// Priority: override local assets, override assets, local assets.
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="ns">The namespace.</param>
        /// <param name="name">The asset name.</param>
        /// <returns>A task with the asset, or null if not found.</returns>
        public UniTask<T> GetAssetAsync<T>(string ns, string name)
            where T : Object;

        /// <summary>
        /// Asynchronously checks if an asset exists by name.
        /// Priority: override local assets, override assets, local assets.
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="name">The asset name.</param>
        /// <returns>A task with a boolean indicating if the asset exists.</returns>
        public UniTask<bool> HasAssetAsync<T>(string name)
            where T : Object;

        /// <summary>
        /// Asynchronously gets an asset by name.
        /// Priority: override local assets, override assets, local assets.
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="name">The asset name.</param>
        /// <returns>A task with the asset, or null if not found.</returns>
        public UniTask<T> GetAssetAsync<T>(string name)
            where T : Object;

        /// <summary>
        /// Asynchronously checks if a local asset exists (from the current mod only).
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="name">The asset name.</param>
        /// <returns>A task with a boolean indicating if the local asset exists.</returns>
        public UniTask<bool> HasLocalAssetAsync<T>(string name) where T : Object;
        
        /// <summary>
        /// Asynchronously gets a local asset (from the current mod only).
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="name">The asset name.</param>
        /// <returns>A task with the local asset, or null if not found.</returns>
        public UniTask<T> GetLocalAssetAsync<T>(string name) where T : Object;

        /// <summary>
        /// Asynchronously checks if an override asset exists in the specified namespace.
        /// Priority: override local assets first.
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="ns">The namespace.</param>
        /// <param name="name">The asset name.</param>
        /// <returns>A task with a boolean indicating if the override asset exists.</returns>
        public UniTask<bool> HasOverrideAssetAsync<T>(string ns, string name) where T : Object;
        
        /// <summary>
        /// Asynchronously gets an override asset from the specified namespace.
        /// Priority: override local assets first.
        /// </summary>
        /// <typeparam name="T">The type of asset.</typeparam>
        /// <param name="ns">The namespace.</param>
        /// <param name="name">The asset name.</param>
        /// <returns>A task with the override asset, or null if not found.</returns>
        public UniTask<T> GetOverrideAssetAsync<T>(string ns, string name) where T : Object;


        /// <summary>
        /// Checks if a world (scene) exists in the specified namespace.
        /// </summary>
        /// <param name="ns">The namespace.</param>
        /// <param name="name">The world name.</param>
        /// <returns>True if the world exists; otherwise, false.</returns>
        public bool HasWorld(string ns, string name);
        
        /// <summary>
        /// Checks if a world (scene) from the specified namespace is currently loaded.
        /// </summary>
        /// <param name="ns">The namespace.</param>
        /// <param name="name">The world name.</param>
        /// <returns>True if the world is loaded; otherwise, false.</returns>
        public bool IsLoadedWorld(string ns, string name);
        
        /// <summary>
        /// Gets a loaded world (scene) from the specified namespace.
        /// </summary>
        /// <param name="ns">The namespace.</param>
        /// <param name="name">The world name.</param>
        /// <returns>The scene object.</returns>
        public Scene GetWorld(string ns, string name);
        
        /// <summary>
        /// Asynchronously loads a world (scene) from the specified namespace.
        /// </summary>
        /// <param name="ns">The namespace.</param>
        /// <param name="name">The world name.</param>
        /// <param name="mode">The load scene mode.</param>
        /// <returns>A task with the loaded scene.</returns>
        public UniTask<Scene> LoadWorld(string ns, string name, LoadSceneMode mode = LoadSceneMode.Single);
        
        /// <summary>
        /// Asynchronously unloads a world (scene) from the specified namespace.
        /// </summary>
        /// <param name="ns">The namespace.</param>
        /// <param name="name">The world name.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public UniTask UnloadWorld(string ns, string name);

        /// <summary>
        /// Checks if a world (scene) exists by name.
        /// </summary>
        /// <param name="name">The world name.</param>
        /// <returns>True if the world exists; otherwise, false.</returns>
        public bool HasWorld(string name);
        
        /// <summary>
        /// Checks if a world (scene) is currently loaded by name.
        /// </summary>
        /// <param name="name">The world name.</param>
        /// <returns>True if the world is loaded; otherwise, false.</returns>
        public bool IsLoadedWorld(string name);
        
        /// <summary>
        /// Gets a loaded world (scene) by name.
        /// </summary>
        /// <param name="name">The world name.</param>
        /// <returns>The scene object.</returns>
        public Scene GetWorld(string name);
        
        /// <summary>
        /// Asynchronously loads a world (scene) by name.
        /// </summary>
        /// <param name="name">The world name.</param>
        /// <param name="mode">The load scene mode.</param>
        /// <returns>A task with the loaded scene.</returns>
        public UniTask<Scene> LoadWorld(string name, LoadSceneMode mode = LoadSceneMode.Single);
        
        /// <summary>
        /// Asynchronously unloads a world (scene) by name.
        /// </summary>
        /// <param name="name">The world name.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public UniTask UnloadWorld(string name);

        /// <summary>
        /// Checks if a local world (scene) exists (from the current mod only).
        /// </summary>
        /// <param name="name">The world name.</param>
        /// <returns>True if the local world exists; otherwise, false.</returns>
        public bool HasLocalWorld(string name);
        
        /// <summary>
        /// Checks if a local world (scene) is currently loaded (from the current mod only).
        /// </summary>
        /// <param name="name">The world name.</param>
        /// <returns>True if the local world is loaded; otherwise, false.</returns>
        public bool IsLoadedLocalWorld(string name);
        
        /// <summary>
        /// Gets a loaded local world (scene) (from the current mod only).
        /// </summary>
        /// <param name="name">The world name.</param>
        /// <returns>The scene object.</returns>
        public Scene GetLocalWorld(string name);
        
        /// <summary>
        /// Asynchronously loads a local world (scene) (from the current mod only).
        /// </summary>
        /// <param name="name">The world name.</param>
        /// <param name="mode">The load scene mode.</param>
        /// <returns>A task with the loaded scene.</returns>
        public UniTask<Scene> LoadLocalWorld(string name, LoadSceneMode mode = LoadSceneMode.Single);
        
        /// <summary>
        /// Asynchronously unloads a local world (scene) (from the current mod only).
        /// </summary>
        /// <param name="name">The world name.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public UniTask UnloadLocalWorld(string name);

        /// <summary>
        /// Checks if an override world (scene) exists in the specified namespace.
        /// </summary>
        /// <param name="ns">The namespace.</param>
        /// <param name="name">The world name.</param>
        /// <returns>True if the override world exists; otherwise, false.</returns>
        public bool HasOverrideWorld(string ns, string name);
        
        /// <summary>
        /// Checks if an override world (scene) from the specified namespace is currently loaded.
        /// </summary>
        /// <param name="ns">The namespace.</param>
        /// <param name="name">The world name.</param>
        /// <returns>True if the override world is loaded; otherwise, false.</returns>
        public bool IsLoadedOverrideWorld(string ns, string name);
        
        /// <summary>
        /// Gets a loaded override world (scene) from the specified namespace.
        /// </summary>
        /// <param name="ns">The namespace.</param>
        /// <param name="name">The world name.</param>
        /// <returns>The scene object.</returns>
        public Scene GetOverrideWorld(string ns, string name);
        
        /// <summary>
        /// Asynchronously loads an override world (scene) from the specified namespace.
        /// </summary>
        /// <param name="ns">The namespace.</param>
        /// <param name="name">The world name.</param>
        /// <param name="mode">The load scene mode.</param>
        /// <returns>A task with the loaded scene.</returns>
        public UniTask<Scene> LoadOverrideWorld(string ns, string name, LoadSceneMode mode = LoadSceneMode.Single);
        
        /// <summary>
        /// Asynchronously unloads an override world (scene) from the specified namespace.
        /// </summary>
        /// <param name="ns">The namespace.</param>
        /// <param name="name">The world name.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public UniTask UnloadOverrideWorld(string ns, string name);
    }
}