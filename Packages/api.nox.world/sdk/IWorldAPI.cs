using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

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
		/// <returns>Returns a <see cref="IScene"/> instance representing the loaded world.</returns>
		public UniTask<IScene> LoadSceneFromPath(string path, Action<float> progress = null, CancellationToken token = default);

		/// <summary>
		/// Loads a world from the given path in the assets.
		/// Return null if the world is not found in the assets.
		/// </summary>
		/// <param name="ns">Namespace of the mod that contains the world.</param>
		/// <param name="path">Path to the world file in the assets.</param>
		/// <param name="progress">Progress callback to report loading progress.</param>
		/// <param name="token">Cancellation token to cancel the loading operation.</param>
		/// <returns>Returns a <see cref="IScene"/> instance representing the loaded world.</returns>
		public UniTask<IScene> LoadSceneFromAssets(string ns, string path, Action<float> progress = null, CancellationToken token = default);

		/// <summary>
		/// Loads a world from the cache using its hash.
		/// If the world is not found in the cache, it will return null.
		/// </summary>
		/// <param name="hash">Hash of the world to load.</param>
		/// <param name="progress">Progress callback to report loading progress.</param>
		/// <param name="token">Cancellation token to cancel the loading operation.</param>
		/// <returns>Returns a <see cref="IScene"/> instance representing the loaded world, or null if not found.</returns>
		public UniTask<IScene> LoadSceneFromCache(string hash, Action<float> progress = null, CancellationToken token = default);

		/// <summary>
		/// Gets the currently active world.
		/// </summary>
		/// <returns></returns>
		public IScene GetCurrent();

		/// <summary>
		/// Sets the current world by its ID.
		/// If the world with the given ID does not exist or cannot be set as current,
		/// If the world is successfully or already set as current, it will return true.
		/// </summary>
		/// <param name="id">Identifier of the world to set as current.</param>
		/// <returns>Returns true if the world was successfully set as current, false otherwise.</returns>
		public bool SetCurrent(string id);

		/// <summary>
		/// Fetches a world by its identifier.
		/// </summary>
		/// <param name="identifier">Identifier of the world to fetch.</param>
		/// <param name="from">Where is the world fetched from, if null it will use the current server.</param>
		/// <returns></returns>
		public UniTask<IWorld> Fetch(string identifier, string from = null);

		/// <summary>
		/// Creates a new search request for worlds.
		/// </summary>
		/// <returns></returns>
		public ISearchRequest MakeSearchRequest();

		/// <summary>
		/// Searches for worlds based on the provided search request.
		/// </summary>
		/// <param name="data">Search request containing the search parameters.</param>
		/// <param name="from">Server where the search is performed, if null it will use the current server.</param>
		/// <returns></returns>
		public UniTask<ISearchResponse> Search(ISearchRequest data, string from = null);

		/// <summary>
		/// Creates a new world based on the provided creation request.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="server"></param>
		/// <returns></returns>
		public UniTask<IWorld> Create(ICreateWorldRequest data, string server);

		/// <summary>
		/// Updates an existing world with the provided update request.
		/// </summary>
		/// <param name="identifier"></param>
		/// <param name="form"></param>
		/// <param name="from"></param>
		/// <returns></returns>
		public UniTask<IWorld> Update(string identifier, IUpdateWorldRequest form, string from = null);

		/// <summary>
		/// Deletes a world by its identifier.
		/// </summary>
		/// <param name="identifier"></param>
		/// <param name="from"></param>
		/// <returns></returns>
		public UniTask<bool> Delete(string identifier, string from = null);

		/// <summary>
		/// Searches for assets associated with a world.
		/// </summary>
		/// <param name="identifier"></param>
		/// <param name="data"></param>
		/// <param name="from"></param>
		/// <returns></returns>
		public UniTask<IAssetSearchResponse> SearchAssets(string identifier, IAssetSearchRequest data, string from = null);

		/// <summary>
		/// Uploads a thumbnail for a world asset.
		/// </summary>
		/// <param name="identifier"></param>
		/// <param name="texture"></param>
		/// <param name="from"></param>
		/// <param name="onProgress"></param>
		/// <returns></returns>
		public UniTask<bool> UploadThumbnail(string identifier, Texture2D texture, string from = null, Action<float> onProgress = null);

		/// <summary>
		/// Uploads a file for a world asset.
		/// </summary>
		/// <param name="identifier"></param>
		/// <param name="assetId"></param>
		/// <param name="fileData"></param>
		/// <param name="fileName"></param>
		/// <param name="fileHash"></param>
		/// <param name="from"></param>
		/// <param name="onProgress"></param>
		/// <returns></returns>
		public UniTask<bool> UploadAssetFile(string identifier, uint assetId, byte[] fileData, string fileName, string fileHash = null, string from = null, Action<float> onProgress = null);

		/// <summary>
		/// Creates a new asset for a world.
		/// </summary>
		/// <param name="identifier"></param>
		/// <param name="data"></param>
		/// <param name="from"></param>
		/// <returns></returns>
		public UniTask<IWorldAsset> CreateAsset(string identifier, ICreateAssetRequest data, string from = null);

		/// <summary>
		/// Downloads a file for a world asset.
		/// </summary>
		/// <param name="identifier"></param>
		/// <param name="assetId"></param>
		/// <param name="hash"></param>
		/// <param name="from"></param>
		/// <param name="progress"></param>
		/// <returns></returns>
		public ICaching DownloadSceneToCache(string identifier, uint assetId, string hash = null, string from = null, UnityAction<float> progress = null);

		/// <summary>
		/// Removes an asset from the cache using its hash.
		/// </summary>
		/// <param name="hash"></param>
		public void RemoveSceneFromCache(string hash);

		/// <summary>
		/// Checks if an asset with the given hash exists in the cache.
		/// </summary>
		/// <param name="hash"></param>
		/// <returns></returns>
		public bool HasSceneInCache(string hash);

		/// <summary>
		/// Creates a world identifier from a string.
		/// </summary>
		/// <param name="identifier"></param>
		/// <returns></returns>
		public IWorldIdentifier Make(string identifier);

		/// <summary>
		/// Adds a world to the favorites list.
		/// </summary>
		/// <param name="identifier">Identifier of the world to add to favorites.</param>
		/// <param name="from">Server where the favorite is stored, if null it will use the current server.</param>
		/// <returns></returns>
		public UniTask<IWorldIdentifier[]> AddFavorite(string identifier, string from = null);

		/// <summary>
		/// Removes a world from the favorites list.
		/// </summary>
		/// <param name="identifier">Identifier of the world to remove from favorites.</param>
		/// <param name="from">Server where the favorite is stored, if null it will use the current server.</param>
		/// <returns></returns>
		public UniTask<IWorldIdentifier[]> RemoveFavorite(string identifier, string from = null);

		/// <summary>
		/// Gets the list of favorite world identifiers.
		/// </summary>
		/// <param name="from">Server where the favorites are stored, if null it will use the current server.</param>
		/// <returns></returns>
		public UniTask<IWorldIdentifier[]> GetFavorites(string from = null);
	}
}