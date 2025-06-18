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
using Nox.Users;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Nox.Worlds;
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

		public async UniTask<IWorld> Fetch(string id, string from = null) {
			await UniTask.Yield();
			return null;
		}

		[NoxPublic(NoxAccess.Method)]
		public IScene GetCurrent()
			=> GroupManager.GetCurrent();
	}
}