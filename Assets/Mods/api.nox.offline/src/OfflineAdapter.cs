using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Entities;
using Nox.Offline;
using Nox.Players;
using Nox.Sessions;
using Nox.Worlds;

namespace api.nox.offline {
	public class OfflineAdapter : IOfflineAdapter {
		internal IWorld         World;
		internal IEntityManager Entities;

		internal OfflineAdapter(IWorld world) {
			World    = world;
			Entities = Main.EntityAPI.New();
		}

		public void SetSession(ISession session) { }

		public async UniTask Dispose() {
			await UniTask.Yield();
			foreach (var entity in Entities.GetEntities().ToArray())
				Entities.UnregisterEntity(entity);
			World.Dispose();
		}

		public IPlayer GetPlayer(int id)
			=> Entities.GetEntity<IPlayer>(id);

		public IEntity GetEntity(int index)
			=> Entities.GetEntity(index);

		public int GetEntityCount()
			=> Entities.GetCount();

		public int GetPlayerCount()
			=> Entities.GetCount<IPlayer>();

		public IWorld GetWorld()
			=> World;

		public override string ToString()
			=> $"{GetType().Name}[World={World}, Entities={Entities}]";
	}
}