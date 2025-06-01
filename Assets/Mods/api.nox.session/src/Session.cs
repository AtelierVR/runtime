using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Entities;
using Nox.Players;
using Nox.Sessions;

namespace api.nox.session {
	public class Session : ISession, INoxObject {
		public Session(Main manager, ushort id, IAdapter adapter) {
			Id      = id;
			Adapter = adapter;
			Adapter.SetSession(this);
			Manager = manager;
			Manager.Add(this);
		}

		public readonly ushort   Id;
		public readonly IAdapter Adapter;
		public readonly Main     Manager;

		[NoxPublic(NoxAccess.Method)]
		public ushort GetId()
			=> Id;

		[NoxPublic(NoxAccess.Method)]
		public IAdapter GetAdapter()
			=> Adapter;

		[NoxPublic(NoxAccess.Method)]
		public void SetCurrent()
			=> Manager.SetCurrent(Id);

		[NoxPublic(NoxAccess.Method)]
		public bool IsCurrent()
			=> Manager.GetCurrent()?.GetId() == Id;

		[NoxPublic(NoxAccess.Method)]
		public IPlayer GetPlayer(int id)
			=> Adapter.GetPlayer(id);

		[NoxPublic(NoxAccess.Method)]
		public IEntity GetEntity(int id)
			=> Adapter.GetEntity(id);

		[NoxPublic(NoxAccess.Method)]
		public int GetEntityCount()
			=> Adapter.GetEntityCount();

		[NoxPublic(NoxAccess.Method)]
		public int GetPlayerCount()
			=> Adapter.GetPlayerCount();

		[NoxPublic(NoxAccess.Method)]
		public async UniTask Dispose() {
			await Adapter.Dispose();
			Manager.Remove(this);
		}

		public void OnPlayerJoined(IPlayer player) {
			Logger.LogDebug($"OnPlayerJoined: {player}");
			Main.CoreAPI.EventAPI.Emit("session_player_joined", this, player);
		}

		public void OnPlayerLeft(IPlayer player) {
			Logger.LogDebug($"OnPlayerLeft: {player}");
			Main.CoreAPI.EventAPI.Emit("session_player_left", this, player);
		}

		public void OnAuthorityTransferred(IPlayer player) {
			Logger.LogDebug($"OnAuthorityTransferred: {player}");
			Main.CoreAPI.EventAPI.Emit("session_authority_transferred", this, player);
		}

		public override string ToString()
			=> $"{GetType().Name}[Id={Id}, Adapter={Adapter}]";

		public void OnDeselect(Session nSession)
			=> Adapter.OnDeselect(nSession);

		public void OnSelect(Session oSession)
			=> Adapter.OnSelect(oSession);
	}
}