using System;
using System.Linq;
using api.nox.offline;
using api.nox.relay.connection;
using api.nox.relay.Instances;
using api.nox.relay.types.Enter;
using api.nox.relay.types.Join;
using api.nox.relay.types.Leave;
using api.nox.relay.types.Player;
using api.nox.relay.types.Quit;
using api.nox.relay.types.Traveling;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Entities;
using Nox.Players;
using Nox.Sessions;
using Nox.Worlds;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Object = System.Object;

namespace api.nox.relay {
	public class RelayAdapter : IAdapter, INoxObject {
		internal         RelayDimension Dimension;
		private readonly IEntityManager _entities;
		private          ISession       _session;
		private          RelayState     _state = new(true);
		internal         Connection     Connection;
		internal         RelayInstance  Instance;

		private  bool       _isTraveling = true;
		internal byte       Tps          = 0;
		private  DateTime   _lastUpdate  = DateTime.MinValue;
		internal float      Threshold    = 0.001f;
		internal float      RenderEntity = 100f;
		internal GameObject EntitiesRoot;


		internal RelayAdapter() {
			Dimension    = null;
			_entities    = Main.EntityAPI.New();
			EntitiesRoot = new GameObject($"[{GetType().Name}Entities]");
			UnityEngine.Object.DontDestroyOnLoad(EntitiesRoot);
		}


		public void OnEnter(EnterResponse ev) {
			Tps          = ev.Tps;
			Threshold    = ev.Threshold;
			RenderEntity = ev.RenderEntity;
			Instance.RequestTraveling(TravelingAction.Travel).Forget();
		}

		public void OnJoin(JoinEvent ev) {
			Logger.LogDebug($"OnJoin: {ev}");
			NewPlayer<RelayRemotePlayer>(ev.Player);
		}

		public void OnLeave(LeaveEvent ev) {
			Logger.LogDebug($"OnLeave: {ev}");
			var player = _entities.GetEntity<RelayRemotePlayer>(ev.PlayerId);
			if (player != null) {
				_entities.UnregisterEntity(player);
			}
		}

		public void OnUpdate() {
			var local = _entities.GetEntities<RelayLocalPlayer>().FirstOrDefault();
			var other = _entities.GetEntities<RelayRemotePlayer>();
			UpdatePlayerDistance(ref local, ref other);
			UpdatePhysicalPlayers(ref local, ref other);
			if (_isTraveling || Tps == 0 || _lastUpdate.AddSeconds(1f / Tps) > DateTime.UtcNow) return;
			_lastUpdate = DateTime.UtcNow;
			local?.SendTransform();
		}

		private void UpdatePhysicalPlayers(ref RelayLocalPlayer local, ref RelayRemotePlayer[] others) {
			if (local == null || others == null || others.Length == 0) return;
			if (!local.HasPhysical())
				local.MakePhysical();
			foreach (var other in others) {
				var physical = other.HasPhysical();
				if (other.DistanceToLocal > RenderEntity && physical)
					other.DestroyPhysical();
				else if (other.DistanceToLocal <= RenderEntity && !physical)
					other.MakePhysical();
			}
		}

		private static void UpdatePlayerDistance(ref RelayLocalPlayer local, ref RelayRemotePlayer[] others) {
			if (local == null || others == null || others.Length == 0) return;
			foreach (var other in others)
				other.DistanceToLocal = Vector3.Distance(local.GetPosition(), other.GetPosition());
		}

		public void OnQuit(QuitEvent ev) {
			Tps          = 0;
			Threshold    = 0.001f;
			RenderEntity = 100f;
		}

		public void OnTraveling(TravelingEvent ev)
			=> OnTravelingAsync(ev).Forget();

		private async UniTask OnTravelingFailed(TravelingEvent ev, string reason)
			=> await Instance.RequestTraveling(TravelingAction.Failed, reason);

		private async UniTask OnTravelingSuccess(TravelingEvent ev)
			=> await Instance.RequestTraveling(TravelingAction.Ready);

		public async UniTask<bool> OnTravelingAsync(TravelingEvent ev, bool autoResponse = true, Action<float, string> progress = null) {
			string hash;
			string url;

			_isTraveling = true;

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
					_isTraveling = false;
					if (autoResponse)
						await OnTravelingFailed(ev, "No master asset found");
					return false;
				}

				hash = asset.GetHash();
				url  = asset.GetUrl();
			} else {
				progress?.Invoke(0.1f, "The traveling does not contain valid URL or master asset information");
				Logger.LogError($"{ev} does not contain valid URL or master asset information");
				_isTraveling = false;
				if (autoResponse)
					await OnTravelingFailed(ev, "Invalid traveling information");
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
				_isTraveling = false;
				if (autoResponse)
					await OnTravelingFailed(ev, "Failed to load scene");
				return false;
			}

			scene.SetIdentifier(ev.UseMaster ? ev.WorldIdentifier : null);
			SetDimension(scene);
			progress?.Invoke(1f, "World loaded successfully");
			_isTraveling = false;
			if (autoResponse)
				await OnTravelingSuccess(ev);
			return true;
		}

		public void SetDimension(IScene scene) {
			if (scene == null) return;
			Dimension = new RelayDimension(0, scene, true);
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
			EntitiesRoot.name = $"[{GetType().Name}Entities_{session.GetId()}]";
			_session          = session;
		}

		public IDimension GetDimension()
			=> Dimension;

		[NoxPublic(NoxAccess.Method)]
		public async UniTask Dispose() {
			if (Instance   != null) await Instance.RequestQuit();
			if (Connection != null) await Connection.RequestDisconnect();
			foreach (var entity in _entities.GetEntities().ToArray())
				_entities.UnregisterEntity(entity);
			if (Dimension.GetMainIndex() > 0)
				Dimension.GetScene().GetMainScene().RemoveInstance(Dimension.GetMainIndex());
			Dimension = null;
			UnityEngine.Object.Destroy(EntitiesRoot);
			EntitiesRoot = null;
		}

		public IPlayer GetPlayer(int index)
			=> _entities.GetEntity<RelayPlayer>(index);

		public IPlayer GetLocalPlayer()
			=> _entities.GetEntities<RelayLocalPlayer>().FirstOrDefault();

		public IPlayer GetMasterPlayer()
			=> _entities.GetEntities<IPlayer>().FirstOrDefault(p => p.IsMaster());

		public IEntity GetEntity(int index)
			=> _entities.GetEntity(index);

		public int GetEntityCount()
			=> _entities.GetCount<IEntity>();

		public int GetPlayerCount()
			=> _entities.GetCount<RelayPlayer>();

		public async UniTask OnDeselect(ISession newSession) {
			Logger.LogDebug($"OnDeselect: {this}");
			var main = Dimension.GetScene().GetMainScene();
			main?.SetVisibleInstance(Dimension.GetMainIndex(), false, false);
			await UniTask.Yield();
		}

		public async UniTask OnSelect(ISession oldSession) {
			Logger.LogDebug($"OnSelect: {this}");
			if (Dimension == null)
				throw new InvalidOperationException($"No current dimension found for session {this}. Please ensure a dimension is set before selecting the session.");
			// if (GetLocalPlayer() == null) NewPlayer();
			var main = Dimension.GetScene().GetMainScene();
			if (Dimension.GetMainIndex() == 0)
				Dimension.SetMainIndex(await main.MakeInstance());
			Dimension.GetScene().SetCurrent();
			main.SetVisibleInstance(Dimension.GetMainIndex(), true, true);
		}

		public async UniTask<bool> TransferAuthority(IPlayer player) {
			Logger.LogWarning($"Not implemented: {nameof(TransferAuthority)} for {this}");
			await UniTask.Yield();
			return false;
		}

		public void NewPlayer<T>(InstancePlayer player) where T : RelayPlayer, new() {
			var np = new T();
			np.SetReference(player, this);
			_entities.RegisterEntity(np);
		}
	}
}