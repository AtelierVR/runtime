using System.Collections.Generic;
using System.Linq;
using api.nox.offline;
using api.nox.relay.connection;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Entities;
using Nox.Players;
using Nox.Sessions;
using Nox.Worlds;

namespace api.nox.relay {
	public class RelayAdapter : IAdapter, INoxObject {
		private readonly List<RelayDimension> _dimensions;
		private readonly IEntityManager       _entities;
		private          int                  _masterPlayerId;
		private          int                  _nextPlayerId;
		private          ISession             _session;
		private          RelayState           _state = new(true);
		internal         Connection           Connection;

		internal RelayAdapter() {
			_dimensions     = new List<RelayDimension>();
			_masterPlayerId = -1;
			_nextPlayerId   = 0;
			_entities       = Main.EntityAPI.New();
		}


		public void AddDimension(string key, IScene scene) {
			if (scene == null) return;
			_dimensions.Add(new RelayDimension(key, 0, scene, true));
		}

		public IAdapterState GetState()
			=> _state;


		internal void SetState(bool isReady, string message = "", float progress = 1f) {
			var old = _state;
			_state = new RelayState(isReady, message, progress);
			_session.OnStateChanged(_state, old);
			Logger.LogDebug($"SetState: {this} -> {_state}");
		}

		public void RemoveDimension(string key)
			=> _dimensions.RemoveAll(e => e.GetName() == key);

		[NoxPublic(NoxAccess.Method)]
		public void SetSession(ISession session) {
			Logger.LogDebug($"SetSession: {this} -> {session}");
			_session = session;
		}

		public IDimension[] GetDimensions()
			=> GetInternalDimensions().Cast<IDimension>().ToArray();

		public IDimension GetCurrentDimension()
			=> GetInternalCurrentDimension();

		private RelayDimension GetInternalCurrentDimension()
			=> GetInternalDimensions().FirstOrDefault(e => e.IsActive());


		private RelayDimension[] GetInternalDimensions()
			=> _dimensions.ToArray();

		public UniTask Dispose() {
			throw new System.NotImplementedException();
		}

		public IPlayer GetPlayer(int index) {
			throw new System.NotImplementedException();
		}

		public IPlayer GetLocalPlayer() {
			throw new System.NotImplementedException();
		}

		public IPlayer GetMasterPlayer() {
			throw new System.NotImplementedException();
		}

		public IEntity GetEntity(int index) {
			throw new System.NotImplementedException();
		}

		public int GetEntityCount() {
			throw new System.NotImplementedException();
		}

		public int GetPlayerCount() {
			throw new System.NotImplementedException();
		}


		public void SetCurrentDimension(string key) {
			throw new System.NotImplementedException();
		}

		public UniTask OnDeselect(ISession newSession) {
			throw new System.NotImplementedException();
		}

		public UniTask OnSelect(ISession oldSession) {
			throw new System.NotImplementedException();
		}

		public UniTask<bool> TransferAuthority(IPlayer player) {
			throw new System.NotImplementedException();
		}
	}
}