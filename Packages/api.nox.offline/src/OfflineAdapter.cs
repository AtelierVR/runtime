using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Entities;
using Nox.Offline;
using Nox.Players;
using Nox.Sessions;
using Nox.Worlds;

namespace api.nox.offline {
	public class OfflineAdapter : IOfflineAdapter {
		private          OfflineDimension _dimension;
		private readonly IEntityManager   _entities;
		private          int              _masterPlayerId;
		private          int              _nextPlayerId;
		private          ISession         _session;
		private          OfflineState     _state = new(true);

		internal OfflineAdapter() {
			_dimension      = null;
			_masterPlayerId = -1;
			_nextPlayerId   = 0;
			_entities       = Main.EntityAPI.New();
		}

		public void SetDimension(IRuntimeWorld runtimeWorld) {
			if (runtimeWorld == null) return;
			_dimension = new OfflineDimension(0, runtimeWorld, true);
		}

		public IAdapterState GetState()
			=> _state;

		internal void SetState(bool isReady, string message = "", float progress = 1f) {
			var old = _state;
			_state = new OfflineState(isReady, message, progress);
			_session.OnStateChanged(_state, old);
		}

		private void NewPlayer() {
			var offlinePlayer = new OfflinePlayer(this, _nextPlayerId++);
			_entities.RegisterEntity(offlinePlayer);
			if (_masterPlayerId < 0)
				TransferOfflineAuthority(offlinePlayer);
			_session.OnPlayerJoined(offlinePlayer);
		}

		private void RemovePlayer(IPlayer player) {
			if (player == null) return;
			_entities.UnregisterEntity(player);
			if (player.GetId() == _masterPlayerId) {
				var newMaster = _entities.GetEntities<OfflinePlayer>()
					.OrderBy(p => p.CreationTime)
					.FirstOrDefault();
				TransferOfflineAuthority(newMaster);
			}

			_session.OnPlayerLeft(player);
		}

		private bool TransferOfflineAuthority(IPlayer player) {
			if (player == null || !_entities.HasEntity(player.GetId())) {
				Logger.LogWarning($"TransferAuthority: Player {player} is not registered.");
				return false;
			}

			if (player.GetId() == _masterPlayerId) {
				Logger.LogWarning($"TransferAuthority: Player {player} is already the master player.");
				return true;
			}

			_masterPlayerId = player.GetId();
			Logger.LogDebug($"TransferAuthority: Authority transferred to player {player}.");
			_session.OnAuthorityTransferred(player);
			return true;
		}

		public IDimension GetDimension()
			=> _dimension;

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
			if (GetLocalPlayer() == null) NewPlayer();
			var main = _dimension.GetScene().GetMainScene();
			if (_dimension.GetMainIndex() == 0)
				_dimension.SetMainIndex(await main.MakeInstance());
			_dimension.GetScene().SetCurrent();
			main.SetVisibleInstance(_dimension.GetMainIndex(), true, true);
		}

		[NoxPublic(NoxAccess.Method)]
		public void SetSession(ISession session) {
			Logger.LogDebug($"SetSession: {this} -> {session}");
			_session = session;
		}

		[NoxPublic(NoxAccess.Method)]
		public async UniTask Dispose() {
			await UniTask.Yield();
			foreach (var entity in _entities.GetEntities().ToArray())
				_entities.UnregisterEntity(entity);
			if (_dimension.GetMainIndex() > 0)
				_dimension.GetScene().GetMainScene().RemoveInstance(_dimension.GetMainIndex());
		}

		[NoxPublic(NoxAccess.Method)]
		public IPlayer GetPlayer(int id)
			=> _entities.GetEntity<IPlayer>(id);

		[NoxPublic(NoxAccess.Method)]
		public IPlayer GetLocalPlayer()
			=> _entities.GetEntities<IPlayer>().FirstOrDefault(p => p.IsLocal());

		[NoxPublic(NoxAccess.Method)]
		public IPlayer GetMasterPlayer()
			=> _entities.GetEntity<IPlayer>(_masterPlayerId);

		[NoxPublic(NoxAccess.Method)]
		public UniTask<bool> TransferAuthority(IPlayer player)
			=> UniTask.FromResult(TransferOfflineAuthority(player));

		[NoxPublic(NoxAccess.Method)]
		public IEntity GetEntity(int index)
			=> _entities.GetEntity(index);

		[NoxPublic(NoxAccess.Method)]
		public int GetEntityCount()
			=> _entities.GetCount();

		[NoxPublic(NoxAccess.Method)]
		public int GetPlayerCount()
			=> _entities.GetCount<IPlayer>();

		public override string ToString()
			=> $"{GetType().Name}[Entities={_entities}, Dimensions={_dimension?.ToString() ?? "null"}]";
	}
}