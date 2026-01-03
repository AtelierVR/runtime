using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using api.nox.world.network;
using api.nox.world.search;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.CCK.Worlds;
using Nox.Network;
using Nox.Search;
using Nox.Sessions;
using Nox.Tables;
using Nox.Users;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Nox.Worlds;
using UnityEngine;
using Cache = api.nox.world.cache.Cache;
using ISearchRequest = Nox.Worlds.ISearchRequest;
using ISearchResponse = Nox.Worlds.ISearchResponse;
using Logger = Nox.CCK.Utils.Logger;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace api.nox.world {
	public class Main : IMainModInitializer, IWorldAPI {
		#region Variables

		internal static Main              Instance;
		internal        IModCoreAPI       CoreAPI;
		internal        SceneGroupManager GroupManager;
		internal        Network           Network;
		internal        Cache             Cache;
		private         LanguagePack      _lang;
		private         Search            _search;

		internal INetworkAPI NetworkAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("network")
				?.GetInstance<INetworkAPI>();

		internal IUserAPI UserAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("user")
				?.GetInstance<IUserAPI>();

		internal ISearchAPI SearchAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("search")
				?.GetInstance<ISearchAPI>();

		internal ITableAPI TableAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("table")
				?.GetInstance<ITableAPI>();

		internal ISessionAPI SessionAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("session")
				?.GetInstance<ISessionAPI>();

		public readonly UnityEvent<IWorldDescriptor, Scene> OnWorldLoaded = new();

		#endregion

		#region ModInitializer

		public void OnInitialize(IModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;
			_lang    = CoreAPI.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_lang);
			GroupManager                =  new SceneGroupManager();
			_search                     =  new Search();
			Network                     =  new Network();
			Cache                       =  new Cache();
			USceneManager.sceneLoaded   += OnSceneLoaded;
			USceneManager.sceneUnloaded += OnSceneUnloaded;
		}

		public async UniTask OnDisposeAsync() {
			LanguageManager.RemovePack(_lang);
			if (GroupManager != null)
				await GroupManager.Dispose();
			GroupManager = null;
			_search?.Dispose();
			Cache?.Dispose();
			CoreAPI  = null;
			Instance = null;

			USceneManager.sceneLoaded   -= OnSceneLoaded;
			USceneManager.sceneUnloaded -= OnSceneUnloaded;
		}


		private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
			if (!scene.TryGetComponentInChildren<IWorldDescriptor>(out var descriptor)) {
				Logger.LogWarning("WorldSystem.OnSceneLoaded: Scene does not have a valid descriptor.");
				return;
			}

			OnWorldLoaded.Invoke(descriptor, scene);
		}

		private void OnSceneUnloaded(Scene scene) { }

		#endregion

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<IRuntimeWorld> LoadFromPath(string path, Action<float> progress = null, CancellationToken token = default)
			=> await GroupManager.LoadWorldFromPath(path, progress, token);

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<IRuntimeWorld> LoadFromAssets(ResourceIdentifier path, Action<float> progress = null, CancellationToken token = default)
			=> await GroupManager.LoadWorldFromAssets(path, progress, token);

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<IRuntimeWorld> LoadFromCache(string hash, Action<float> progress = null, CancellationToken token = default)
			=> await GroupManager.LoadWorldFromCache(hash, progress, token);

		[NoxPublic(NoxAccess.Method)]
		public bool SetCurrent(string id)
			=> GroupManager.SetCurrent(id);

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<IWorld> Fetch(string identifier, string from = null)
			=> await Network.Fetch(identifier, from);

		public bool HasSceneInCache(string hash)
			=> Cache.Has(hash);

		public IWorldIdentifier Make(uint id,
			Dictionary<string, string[]>  meta   = null,
			string                        server = "::")
			=> new WorldIdentifier(id, meta, server);

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<IWorldIdentifier[]> AddFavorite(string identifier, string from = null)
			=> (await Network.AddFavorite(identifier, from)).Cast<IWorldIdentifier>().ToArray();

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<IWorldIdentifier[]> RemoveFavorite(string identifier, string from = null)
			=> (await Network.RemoveFavorite(identifier, from)).Cast<IWorldIdentifier>().ToArray();

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<IWorldIdentifier[]> GetFavorites(string from = null)
			=> (await Network.FetchFavorites(from)).Cast<IWorldIdentifier>().ToArray();

		[NoxPublic(NoxAccess.Method)]
		public IAssetSearchRequest MakeAssetSearchRequest()
			=> new AssetSearchRequest();

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<ISearchResponse> Search(ISearchRequest data, string from = null)
			=> await Network.Search(SearchRequest.From(data), from);

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<IWorld> Create(ICreateWorldRequest data, string server)
			=> await Network.Create(CreateWorldRequest.FromBase(data), server);

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<IWorld> Update(string identifier, IUpdateWorldRequest form, string from = null)
			=> await Network.Update(identifier, UpdateWorldRequest.FromBase(form), from);

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<bool> Delete(string identifier, string from = null)
			=> await Network.Delete(identifier, from);

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<IAssetSearchResponse> SearchAssets(string identifier, IAssetSearchRequest data, string from = null)
			=> await Network.SearchAssets(identifier, AssetSearchRequest.FromBase(data), from);

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<bool> UploadThumbnail(string identifier, Texture2D texture, string from = null, Action<float> onProgress = null)
			=> await Network.UploadThumbnail(identifier, texture, from, onProgress);

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<bool> UploadAssetFile(string identifier, uint assetId, byte[] fileData, string fileName, string fileHash = null, string from = null, Action<float> onProgress = null)
			=> await Network.UploadAssetFile(identifier, assetId, fileData, fileName, fileHash, from, onProgress);

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<IWorldAsset> CreateAsset(string identifier, ICreateAssetRequest data, string from = null)
			=> await Network.CreateAsset(identifier, CreateAssetRequest.FromBase(data), from);

		[NoxPublic(NoxAccess.Method)]
		public IRuntimeWorld GetCurrent()
			=> GroupManager.GetCurrent();

		[NoxPublic(NoxAccess.Method)]
		public ICaching DownloadSceneToCache(string url, string hash = null, string from = null, UnityAction<float> progress = null) {
			var caching = Cache.AddDownload(url, hash);
			if (progress != null) caching.OnProgress.AddListener(progress);
			return caching;
		}

		[NoxPublic(NoxAccess.Method)]
		public void RemoveSceneFromCache(string hash)
			=> Cache.Clear(hash);
	}
}