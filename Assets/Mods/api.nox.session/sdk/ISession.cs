using Cysharp.Threading.Tasks;
using Nox.Entities;
using Nox.Players;

namespace Nox.Sessions {
	public interface ISession {
		public IAdapter GetAdapter();

		public IPlayer GetPlayer(int id);
		public IEntity GetEntity(int id);
		public int     GetEntityCount();
		public int     GetPlayerCount();

		public UniTask Dispose();
	}
}