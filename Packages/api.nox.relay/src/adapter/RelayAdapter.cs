using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.offline;
using api.nox.relay.connection;
using api.nox.relay.Instances;
using api.nox.relay.types.Enter;
using api.nox.relay.types.Player;
using api.nox.relay.types.Quit;
using api.nox.relay.types.Traveling;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Entities;
using Nox.Players;
using Nox.Sessions;
using Nox.Worlds;
using src.adapter.players;

namespace api.nox.relay {
	public class RelayAdapter : IAdapter, INoxObject {
		private          RelayDimension _dimension;
		private readonly IEntityManager _entities;
		private          ISession       _session;
		private          RelayState     _state = new(true);
		internal         Connection     Connection;
		internal         RelayInstance  Instance;

		internal RelayAdapter() {
			_dimension = null;
			_entities  = Main.EntityAPI.New();
		}

		public void OnQuit(QuitEvent ev) { }

		public void OnTraveling(TravelingEvent ev)
			=> OnTravelingAsync(ev).Forget();

		public async UniTask<bool> OnTravelingAsync(TravelingEvent ev, Action<float, string> progress = null) {
			string hash;
			string url;

			if (ev.UseUrl) {
				progress?.Invoke(0.1f, "Using provided URL for world travel");
				hash = ev.Hash;
				url  = ev.DownloadUrl;
			} else if (ev.UseMaster) {
				progress?.Invoke(0.1f, "Searching for master asset for world travel");
				var asset = (await Main.WorldAPI.SearchAssets(
						ev.WorldIdentifier.GetId().ToString(),
						Main.WorldAPI.MakeAssetSearchRequest()
							.SetEngines(new[] { EngineExtensions.CurrentEngine.GetEngineName() })
							.SetPlatforms(new[] { PlatformExtensions.CurrentPlatform.GetPlatformName() })
							.SetVersions(new[] { ev.WorldIdentifier.GetVersion() })
							.SetLimit(1),
						ev.WorldIdentifier.GetServerAddress()
					)).GetAssets()
					.FirstOrDefault();

				if (asset == null) {
					progress?.Invoke(0.2f, $"No master asset found for world {ev.WorldIdentifier.ToString()}");
					Logger.LogError($"No asset found for world {ev.WorldIdentifier.ToString()}");
					return false;
				}

				hash = asset.GetHash();
				url  = asset.GetUrl();
			} else {
				progress?.Invoke(0.1f, "The traveling does not contain valid URL or master asset information");
				Logger.LogError($"{ev} does not contain valid URL or master asset information");
				return false;
			}

			progress?.Invoke(0.2f, "Searching for world");
			if (!Main.WorldAPI.HasSceneInCache(hash)) {
				var download = Main.WorldAPI.DownloadSceneToCache(
					url,
					hash: hash,
					progress: f => progress?.Invoke(0.2f + f * 0.45f, "Downloading world...")
				);
				download.Start();
				await download.Wait();
			}

			progress?.Invoke(0.65f, "Loading world");
			var scene = await Main.WorldAPI.LoadSceneFromCache(
				hash,
				progress: f => progress?.Invoke(0.65f + f * 0.25f, "Loading world...")
			);
			if (scene == null) {
				progress?.Invoke(0.9f, "Failed to load scene for world");
				Logger.LogError($"Failed to load scene for world {ev.WorldIdentifier.ToString()}");
				return false;
			}

			scene.SetIdentifier(ev.UseMaster ? ev.WorldIdentifier : null);
			SetDimension(scene);

			progress?.Invoke(1f, "World loaded successfully");
			return true;
		}

		public void OnEnter(EnterResponse ev) {
			Instance.RequestTraveling(TravelingAction.Travel).Forget();
		}

		public void SetDimension(IScene scene) {
			if (scene == null) return;
			_dimension = new RelayDimension(0, scene, true);
		}

		public IAdapterState GetState()
			=> _state;

		internal void SetState(bool isReady, string message = "", float progress = 1f) {
			var old = _state;
			_state = new RelayState(isReady, message, progress);
			_session.OnStateChanged(_state, old);
			Logger.LogDebug($"SetState: {this} -> {_state}");
		}

		[NoxPublic(NoxAccess.Method)]
		public void SetSession(ISession session) {
			Logger.LogDebug($"SetSession: {this} -> {session}");
			_session = session;
		}

		public IDimension GetDimension()
			=> _dimension;

		[NoxPublic(NoxAccess.Method)]
		public async UniTask Dispose() {
			if (Instance   != null) await Instance.RequestQuit();
			if (Connection != null) await Connection.RequestDisconnect();
			foreach (var entity in _entities.GetEntities().ToArray())
				_entities.UnregisterEntity(entity);
			if (_dimension.GetMainIndex() > 0)
				_dimension.GetScene().GetMainScene().RemoveInstance(_dimension.GetMainIndex());
		}

		public IPlayer GetPlayer(int index)
			=> _entities.GetEntity<IPlayer>(index);

		public IPlayer GetLocalPlayer()
			=> _entities.GetEntities<IPlayer>().FirstOrDefault(p => p.IsLocal());

		public IPlayer GetMasterPlayer()
			=> _entities.GetEntities<IPlayer>().FirstOrDefault(p => p.IsMaster());

		public IEntity GetEntity(int index)
			=> _entities.GetEntity(index);

		public int GetEntityCount()
			=> _entities.GetCount<IEntity>();

		public int GetPlayerCount()
			=> _entities.GetCount<IPlayer>();

		public async UniTask OnDeselect(ISession newSession) {
			Logger.LogDebug($"OnDeselect: {this}");
			var main = _dimension.GetScene().GetMainScene();
			main?.SetVisibleInstance(_dimension.GetMainIndex(), false, false);
			await UniTask.Yield();
		}

		public async UniTask OnSelect(ISession oldSession) {
			Logger.LogDebug($"OnSelect: {this}");
			if (_dimension == null)
				throw new InvalidOperationException($"No current dimension found for session {this}. Please ensure a dimension is set before selecting the session.");
			// if (GetLocalPlayer() == null) NewPlayer();
			var main = _dimension.GetScene().GetMainScene();
			if (_dimension.GetMainIndex() == 0)
				_dimension.SetMainIndex(await main.MakeInstance());
			_dimension.GetScene().SetCurrent();
			main.SetVisibleInstance(_dimension.GetMainIndex(), true, true);
		}

		public async UniTask<bool> TransferAuthority(IPlayer player) {
			Logger.LogWarning($"Not implemented: {nameof(TransferAuthority)} for {this}");
			await UniTask.Yield();
			return false;
		}

		public void NewPlayer<T>(InstancePlayer player) where T : RelayPlayer, new() {
			var np = new T();
			np.SetReference(player);
			_entities.RegisterEntity(np);
		}
	}
}