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
		private readonly IScene         _scene;
		private readonly IEntityManager _entities;
		private          int            _masterPlayerId;
		private          int            _nextPlayerId;
		private          ISession       _session;
		private          int            _sceneId = 0;

		internal OfflineAdapter(IScene scene) {
			_scene          = scene;
			_masterPlayerId = -1;
			_nextPlayerId   = 0;
			_entities       = Main.EntityAPI.New();
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
			if (player.GetId() != _masterPlayerId) return;
			var newMaster = _entities.GetEntities<OfflinePlayer>()
				.OrderBy(p => p.CreationTime)
				.FirstOrDefault();
			TransferOfflineAuthority(newMaster);
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

		public async UniTask OnDeselect(ISession newSession) {
			Logger.LogDebug($"OnDeselect: {this}");
			var main = _scene.GetMainScene();
			main?.SetVisibleInstance(_sceneId, false, false);
			await UniTask.Yield();
		}

		public async UniTask OnSelect(ISession oldSession) {
			Logger.LogDebug($"OnSelect: {this}");
			if (GetLocalPlayer() == null) NewPlayer();
			var main = _scene.GetMainScene();
			if (_sceneId == 0)
				_sceneId = await main.MakeInstance();
			_scene.SetCurrent();
			main.SetVisibleInstance(_sceneId, true, true);
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
			await _scene.Dispose();
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

		[NoxPublic(NoxAccess.Method)]
		public IScene GetWorld()
			=> _scene;

		public override string ToString()
			=> $"{GetType().Name}[World={_scene}, Entities={_entities}]";
	}
}