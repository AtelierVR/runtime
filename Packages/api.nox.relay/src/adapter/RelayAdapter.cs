using System;
using System.Linq;
using api.nox.offline;
using api.nox.relay.connection;
using api.nox.relay.Instances;
using api.nox.relay.types.Avatar;
using api.nox.relay.types.Enter;
using api.nox.relay.types.Join;
using api.nox.relay.types.Leave;
using api.nox.relay.types.Player;
using api.nox.relay.types.Quit;
using api.nox.relay.types.Transform;
using api.nox.relay.types.Traveling;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.CCK.Utils;
using Nox.Entities;
using Nox.Players;
using Nox.Sessions;
using Nox.Worlds;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.relay {
	public class RelayAdapter : IAdapter, INoxObject {
		private          RelayDimension    _dimension;
		private readonly IEntityManager    _entities;
		private          ISession          _session;
		private          RelayState        _state = new(true);
		internal         Connection        Connection;
		internal         RelayInstance     Instance;
		internal         IAvatarIdentifier Avatar;

		private  bool       _isTraveling  = true;
		internal byte       Tps           = 24;
		private  DateTime   _lastUpdate   = DateTime.MinValue;
		internal float      Threshold     = 0.001f;
		private  float      _renderEntity = 100f;
		internal GameObject EntitiesRoot;
		internal string     Name;
		internal string     ShortName;
		internal Texture2D  Thumbnail;

		internal RelayAdapter() {
			_dimension   = null;
			_entities    = Main.EntityAPI.New();
			EntitiesRoot = new GameObject($"[{GetType().Name}Entities]");
			UnityEngine.Object.DontDestroyOnLoad(EntitiesRoot);
		}

		public void OnEnter(EnterResponse ev) {
			Tps           = ev.Tps;
			Threshold     = ev.Threshold;
			_renderEntity = ev.RenderEntity;
			Instance.RequestTraveling(TravelingAction.Travel).Forget();
		}

		public void OnJoin(JoinEvent ev) {
			Logger.LogDebug($"OnJoin: {ev}");
			NewPlayer<RelayRemotePlayer>(ev.Player);
		}

		public void OnTransform(TransformEvent ev) {
			switch (ev.Type) {
				case TransformType.Player: {
					var player = _entities.GetEntity<RelayPlayer>(ev.PlayerId);
					if (player == null) return;
					player.MovePart(ev.PlayerRig, ev.Transform, DeliveryType.RemoteModified);
					break;
				}
				case TransformType.Entity: {
					var entity = _entities.GetEntity<RelayEntity>(ev.EntityId);
					if (entity == null) return;
					entity.Move(ev.Transform, DeliveryType.RemoteModified);
					break;
				}
				case TransformType.ByPath: {
					var path = ev.Path.Split(ComponentExtension.PathSeparator);
					if (!int.TryParse(path[0], out var index)) {
						Logger.LogWarning($"Invalid entity path received: {ev.Path}");
						return;
					}

					var scene = _dimension.GetScene().GetInstance(index)?.GetScene();
					if (!scene.HasValue) {
						Logger.LogWarning($"No scene found for entity path: {ev.Path}");
						return;
					}

					var transform = scene.Value.GetByPath(path.Skip(1).ToArray());
					if (!transform) {
						Logger.LogWarning($"No transform found for entity path: {ev.Path}");
						return;
					}

					transform.Move(ev.Transform);
					break;
				}
				default:
					break;
			}
		}

		public void OnAvatarParams(AvatarParamsEvent ev) {
			var player = _entities.GetEntity<RelayPlayer>(ev.PlayerId);
			if (player == null) return;
			foreach (var param in ev.Parameters)
				player.SetParameter(param.Key, param.Value, DeliveryType.RemoteModified);
		}

		public void OnLeave(LeaveEvent ev) {
			Logger.LogDebug($"OnLeave: {ev}");
			var player = _entities.GetEntity<RelayRemotePlayer>(ev.PlayerId);
			if (player == null) return;
			RemovePlayer(player);
		}

		public void OnAvatarChanged(AvatarChangedEvent ev)
			=> OnAvatarChangedAsync(ev).Forget();

		private async UniTask OnAvatarChangedAsync(AvatarChangedEvent ev) {
			if (ev.Result != AvatarChangedResult.Changing) return;
			var player = _entities.GetEntity<RelayPlayer>(ev.PlayerId);
			if (player == null) return;
			await player.SetAvatar(ev.AvatarIdentifier);
		}

		public void OnUpdate() {
			if (!_session.IsCurrent()) return;
			if (_isTraveling || Tps == 0 || _lastUpdate.AddSeconds(1f / Tps) > DateTime.UtcNow) return;
			_lastUpdate = DateTime.UtcNow;
			var local = _entities.GetEntities<RelayLocalPlayer>().FirstOrDefault();
			var other = _entities.GetEntities<RelayRemotePlayer>();
			UpdatePlayerDistance(ref local, ref other);
			UpdatePhysicalPlayers(ref local, ref other);
			local?.SendTransform();
			local?.SendParameters();
		}

		private void UpdatePhysicalPlayers(ref RelayLocalPlayer local, ref RelayRemotePlayer[] others) {
			if (local == null || others == null || others.Length == 0) return;
			if (!local.HasPhysical())
				local.MakePhysical();
			foreach (var other in others) {
				var physical = other.HasPhysical();
				if (other.DistanceToLocal > _renderEntity && physical)
					other.DestroyPhysical();
				else if (other.DistanceToLocal <= _renderEntity && !physical)
					other.MakePhysical();
			}
		}

		private static void UpdatePlayerDistance(ref RelayLocalPlayer local, ref RelayRemotePlayer[] others) {
			if (local == null || others == null || others.Length == 0) return;
			foreach (var other in others)
				other.DistanceToLocal = Vector3.Distance(local.GetPosition(), other.GetPosition());
		}

		public void OnQuit(QuitEvent ev) {
			Tps           = 0;
			Threshold     = 0.001f;
			_renderEntity = 100f;
		}

		public void OnTraveling(TravelingEvent ev)
			=> OnTravelingAsync(ev).Forget();

		private async UniTask OnTravelingFailed(TravelingEvent _, string reason)
			=> await Instance.RequestTraveling(TravelingAction.Failed, reason);

		private async UniTask OnTravelingSuccess(TravelingEvent _)
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
							.SetLimit(1)
					))?.GetAssets()
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
				await download.Start();
			}

			progress?.Invoke(0.65f, "Loading world");
			var scene = await Main.WorldAPI.LoadFromCache(
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

		public void SetDimension(IRuntimeWorld runtimeWorld) {
			if (runtimeWorld == null) return;
			_dimension = new RelayDimension(0, runtimeWorld, true);
		}

		public IAdapterState GetState()
			=> _state;

		internal void SetState(bool isReady, string message = "", float progress = 1f) {
			var old = _state;
			_state = new RelayState(isReady, message, progress);
			_session.OnStateChanged(_state, old);
			Logger.LogDebug($"SetState: {this} -> {_state}");
		}

		public string GetName()
			=> Name;

		public string GetShortName()
			=> ShortName ?? _session.GetId().ToString();


		public async UniTask<Texture2D> GetThumbnail() {
			await UniTask.Yield();
			return Thumbnail;
		}

		[NoxPublic(NoxAccess.Method)]
		public void SetSession(ISession session) {
			Logger.LogDebug($"SetSession: {this} -> {session}");
			EntitiesRoot.name = $"[{GetType().Name}Entities_{session.GetId()}]";
			_session          = session;
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
				_dimension.GetScene().GetInstances()[0].RemoveInstance(_dimension.GetMainIndex());
			_dimension = null;
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
			var main = _dimension.GetScene().GetInstances()[0];
			main?.SetVisibleInstance(_dimension.GetMainIndex(), false, false);
			var entities = _entities.GetEntities().ToArray();
			foreach (var entity in entities)
				entity.DestroyPhysical();
			await UniTask.Yield();
		}

		public async UniTask OnSelect(ISession oldSession) {
			Logger.LogDebug($"OnSelect: {this}");
			if (_dimension == null)
				throw new InvalidOperationException($"No current dimension found for session {this}. Please ensure a dimension is set before selecting the session.");

			var main = _dimension.GetScene().GetInstances()[0];
			if (_dimension.GetMainIndex() == 0) {
				var id = await main.MakeInstance();
				_dimension.SetMainIndex(id);
				_session.OnSceneLoaded(main.GetDescriptor(id), id, main.GetAnchor(id));
			}

			_dimension.GetScene().SetCurrent();
			main.SetVisibleInstance(_dimension.GetMainIndex(), true, true);
		}

		public async UniTask<bool> TransferAuthority(IPlayer player) {
			Logger.LogWarning($"Not implemented: {nameof(TransferAuthority)} for {this}");
			await UniTask.Yield();
			return false;
		}

		public T NewPlayer<T>(InstancePlayer player) where T : RelayPlayer, new() {
			var np = new T();
			np.SetReference(player, this);
			_entities.RegisterEntity(np);
			_session.OnPlayerJoined(np);
			return np;
		}

		private void RemovePlayer(RelayPlayer player) {
			if (player == null) return;
			_entities.UnregisterEntity(player);
			_session.OnPlayerLeft(player);
			player.Dispose();
		}
	}
}