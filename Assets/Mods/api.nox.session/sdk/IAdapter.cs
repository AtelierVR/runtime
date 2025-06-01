using Cysharp.Threading.Tasks;
using Nox.Entities;
using Nox.Players;
using Nox.Worlds;

namespace Nox.Sessions {
	/// <summary>
	/// The Adapter is the comportment with external api and the session.
	/// </summary>
	public interface IAdapter {
		/// <summary>
		/// Set the session for the adapter.
		/// </summary>
		/// <param name="session"></param>
		public void SetSession(ISession session);

		/// <summary>
		/// Close the adapter.
		/// </summary>
		public UniTask Dispose();

		/// <summary>
		/// Get a player by index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public IPlayer GetPlayer(int index);

		/// <summary>
		/// Get an entity by index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public IEntity GetEntity(int index);

		/// <summary>
		/// Get the count of entities in the session.
		/// </summary>
		/// <returns></returns>
		int GetEntityCount();

		/// <summary>
		/// Get the count of players in the session.
		/// </summary>
		/// <returns></returns>
		int GetPlayerCount();

		/// <summary>
		/// Get the world associated with the session.
		/// </summary>
		/// <returns></returns>
		IWorld GetWorld();
	}
}