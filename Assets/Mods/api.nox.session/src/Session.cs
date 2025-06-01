using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Entities;
using Nox.Players;
using Nox.Sessions;

namespace api.nox.session {
	public class Session : ISession, INoxObject {
		public Session(Main manager, IAdapter adapter) {
			Adapter = adapter;
			Adapter.SetSession(this);
			Manager = manager;
			Manager.Add(this);
		}

		public readonly IAdapter Adapter;
		public readonly Main     Manager;

		[NoxPublic(NoxAccess.Method)]
		public IAdapter GetAdapter()
			=> Adapter;

		public IPlayer GetPlayer(int id)
			=> Adapter.GetPlayer(id);

		public IEntity GetEntity(int id)
			=> Adapter.GetEntity(id);

		public int GetEntityCount()
			=> Adapter.GetEntityCount();

		public int GetPlayerCount()
			=> Adapter.GetPlayerCount();

		public async UniTask Dispose() {
			await Adapter.Dispose();
			Manager.Remove(this);
		}

		public override string ToString()
			=> $"{GetType().Name}[Adapter={Adapter}]";
	}
}