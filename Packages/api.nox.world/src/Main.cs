using System;
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
using Nox.Tables;
using Nox.Users;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Nox.Worlds;
using UnityEngine;
using ISearchRequest = Nox.Worlds.ISearchRequest;
using ISearchResponse = Nox.Worlds.ISearchResponse;
using Logger = Nox.CCK.Utils.Logger;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace api.nox.world {
	public class Main : MainModInitializer, IWorldAPI {
		#region Variables

		internal static Main       Instance;
		internal        ModCoreAPI CoreAPI;

		internal INetworkAPI NetworkAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("network")
				?.GetMains()
				.FirstOrDefault() as INetworkAPI;

		internal IUserAPI UserAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("user")
				?.GetMains()
				.FirstOrDefault() as IUserAPI;

		internal ISearchAPI SearchAPI
			=> Main.Instance.CoreAPI.ModAPI
				.GetMod("search")
				.GetMains()
				.FirstOrDefault() as ISearchAPI;

		internal ITableAPI TableAPI
			=> Main.Instance.CoreAPI.ModAPI
				.GetMod("table")
				.GetMains()
				.FirstOrDefault() as ITableAPI;

		internal SceneGroupManager GroupManager;
		internal Network           Network;

		private LanguagePack _lang;
		private Search       _search;


		public readonly UnityEvent<BaseSceneDescriptor, Scene> OnWorldLoaded     = new();
		public readonly UnityEvent<MainSceneDescriptor, Scene> OnMainWorldLoaded = new();
		public readonly UnityEvent<SubSceneDescriptor, Scene>  OnSubWorldLoaded  = new();

		#endregion

		#region ModInitializer

		public void OnInitialize(ModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;
			_lang    = CoreAPI.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_lang);
			GroupManager                =  new SceneGroupManager();
			_search                     =  new Search();
			Network                     =  new Network();
			USceneManager.sceneLoaded   += OnSceneLoaded;
			USceneManager.sceneUnloaded += OnSceneUnloaded;
		}

		public async UniTask OnDisposeAsync() {
			LanguageManager.RemovePack(_lang);
			if (GroupManager != null)
				await GroupManager.Dispose();
			GroupManager = null;
			_search?.Dispose();
			CoreAPI                     =  null;
			Instance                    =  null;
			USceneManager.sceneLoaded   -= OnSceneLoaded;
			USceneManager.sceneUnloaded -= OnSceneUnloaded;
		}


		private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
			if (!SceneDescriptorExtension.TryGetDescriptor<BaseSceneDescriptor>(scene, out var descriptor)) {
				Logger.LogWarning("WorldSystem.OnSceneLoaded: Scene does not have a valid descriptor.");
				return;
			}

			OnWorldLoaded.Invoke(descriptor, scene);
			if (descriptor is MainSceneDescriptor mainDescriptor)
				OnMainWorldLoaded.Invoke(mainDescriptor, scene);
			else if (descriptor is SubSceneDescriptor subDescriptor)
				OnSubWorldLoaded.Invoke(subDescriptor, scene);
		}

		private void OnSceneUnloaded(Scene scene) { }

		#endregion

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<IScene> LoadWorldFromPath(string path, Action<float> progress = null, CancellationToken token = default)
			=> await GroupManager.LoadWorldFromPath(path, progress, token);

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<IScene> LoadWorldFromAssets(string modId, string path, Action<float> progress = null, CancellationToken token = default)
			=> await GroupManager.LoadWorldFromAssets(modId, path, progress, token);

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<IScene> LoadWorldFromCache(string hash, Action<float> progress = null, CancellationToken token = default)
			=> await GroupManager.LoadWorldFromCache(hash, progress, token);

		[NoxPublic(NoxAccess.Method)]
		public bool SetCurrent(string id)
			=> GroupManager.SetCurrent(id);

		public async UniTask<IWorld> Fetch(string identifier, string from = null)
			=> await Network.Fetch(identifier, from);

		public async UniTask<byte[]> DownloadAssetFile(string identifier, uint assetId, string from = null, Action<float> onProgress = null) {
			throw new NotImplementedException();
		}

		public IWorldIdentifier Make(string identifier)
			=> WorldIdentifier.FromString(identifier);

		public async UniTask<IWorldIdentifier[]> AddFavorite(string identifier, string from = null)
			=> await Network.AddFavorite(identifier, from);

		public async UniTask<IWorldIdentifier[]> RemoveFavorite(string identifier, string from = null)
			=> await Network.RemoveFavorite(identifier, from);

		public async UniTask<IWorldIdentifier[]> GetFavorites(string from = null)
			=> await Network.FetchFavorites(from);

		public ISearchRequest MakeSearchRequest()
			=> new SearchRequest();

		public async UniTask<ISearchResponse> Search(ISearchRequest data, string from = null)
			=> await Network.Search(SearchRequest.FromBase(data), from);

		public async UniTask<IWorld> Create(ICreateWorldRequest data, string server)
			=> await Network.Create(CreateWorldRequest.FromBase(data), server);

		public async UniTask<IWorld> Update(string identifier, IUpdateWorldRequest form, string from = null)
			=> await Network.Update(identifier, UpdateWorldRequest.FromBase(form), from);

		public async UniTask<bool> Delete(string identifier, string from = null)
			=> await Network.Delete(identifier, from);

		public async UniTask<IAssetSearchResponse> SearchAssets(string identifier, IAssetSearchRequest data, string from = null)
			=> await Network.SearchAssets(identifier, AssetSearchRequest.FromBase(data), from);

		public async UniTask<bool> UploadThumbnail(string identifier, Texture2D texture, string from = null, Action<float> onProgress = null)
			=> await Network.UploadThumbnail(identifier, texture, from, onProgress);

		public async UniTask<bool> UploadAssetFile(string identifier, uint assetId, byte[] fileData, string fileName, string fileHash = null, string from = null, Action<float> onProgress = null)
			=> await Network.UploadAssetFile(identifier, assetId, fileData, fileName, fileHash, from, onProgress);
		
		public async UniTask<IWorldAsset> CreateAsset(string identifier, ICreateAssetRequest data, string from = null)
			=> await Network.CreateAsset(identifier, CreateAssetRequest.FromBase(data), from);

		[NoxPublic(NoxAccess.Method)]
		public IScene GetCurrent()
			=> GroupManager.GetCurrent();
	}
}