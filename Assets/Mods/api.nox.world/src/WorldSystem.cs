using System;
using System.Linq;
using System.Threading;
using api.nox.world.search;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.CCK.Worlds;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Nox.Worlds;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace api.nox.world {
	public class WorldSystem : MainModInitializer, IWorldAPI {
		#region Mod Imports

		internal static MainModInitializer NetworkAPI
			=> CoreAPI.ModAPI
				.GetMod("network")
				.GetMains()
				.FirstOrDefault();

		internal static INoxObject WorldAPI
			=> NetworkAPI.GetField("World");

		#endregion

		#region Variables

		internal static                             WorldSystem                       Instance;
		internal static                             ModCoreAPI                        CoreAPI;
		private                                     LanguagePack                      _lang;
		private                                     WorldSearch                       _worldSearch;
		[NoxPublic(NoxAccess.Read)] public          WorldManager                      Manager;
		[NoxPublic(NoxAccess.Read)] public readonly UnityEvent<BaseDescriptor, Scene> OnWorldLoaded     = new();
		[NoxPublic(NoxAccess.Read)] public readonly UnityEvent<MainDescriptor, Scene> OnMainWorldLoaded = new();
		[NoxPublic(NoxAccess.Read)] public readonly UnityEvent<SubDescriptor, Scene>  OnSubWorldLoaded  = new();

		#endregion

		#region ModInitializer

		public void OnInitialize(ModCoreAPI api) {
			CoreAPI      = api;
			Instance     = this;
			_worldSearch = new WorldSearch();
			_lang        = CoreAPI.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_lang);
			USceneManager.sceneLoaded   += OnSceneLoaded;
			USceneManager.sceneUnloaded += OnSceneUnloaded;
			Manager                     =  new WorldManager();
		}

		public async UniTask OnDisposeAsync() {
			LanguageManager.RemovePack(_lang);
			if (Manager != null)
				await Manager.Dispose();
			Manager = null;
			_worldSearch?.Dispose();
			_worldSearch                =  null;
			CoreAPI                     =  null;
			Instance                    =  null;
			USceneManager.sceneLoaded   -= OnSceneLoaded;
			USceneManager.sceneUnloaded -= OnSceneUnloaded;
		}


		private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
			if (!BaseDescriptor.TryGetDescriptor<BaseDescriptor>(scene, out var descriptor)) {
				Logger.LogWarning("WorldSystem.OnSceneLoaded: Scene does not have a valid descriptor.");
				return;
			}

			OnWorldLoaded.Invoke(descriptor, scene);
			if (descriptor is MainDescriptor mainDescriptor)
				OnMainWorldLoaded.Invoke(mainDescriptor, scene);
			else if (descriptor is SubDescriptor subDescriptor)
				OnSubWorldLoaded.Invoke(subDescriptor, scene);
		}

		private void OnSceneUnloaded(Scene scene) { }

		#endregion

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<IWorld> LoadWorldFromPath(string path, Action<float> progress = null, CancellationToken token = default)
			=> await Manager.LoadWorldFromPath(path, progress, token);

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<IWorld> LoadWorldFromAssets(string modId, string path, Action<float> progress = null, CancellationToken token = default)
			=> await Manager.LoadWorldFromAssets(modId, path, progress, token);

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<IWorld> LoadWorldFromCache(string hash, Action<float> progress = null, CancellationToken token = default)
			=> await Manager.LoadWorldFromCache(hash, progress, token);

		[NoxPublic(NoxAccess.Method)]
		public bool SetCurrent(string id)
			=> Manager.SetCurrent(id);

		[NoxPublic(NoxAccess.Method)]
		public IWorld GetCurrent()
			=> Manager.GetCurrent();
	}
}