using Cysharp.Threading.Tasks;
using Nox.Entities;
using Nox.Players;
using Nox.Worlds;

namespace Nox.Sessions {
	public interface ISession {
		/// <summary>
		/// Get the id of the session.
		/// </summary>
		/// <returns></returns>
		public ushort GetId();

		/// <summary>
		/// Get the adapter of the session.
		/// </summary>
		/// <returns></returns>
		public IAdapter GetAdapter();

		/// <summary>
		/// Make the session as the current session.
		/// </summary>
		public UniTask SetCurrent();

		/// <summary>
		/// Check if the session is the current session.
		/// </summary>
		/// <returns></returns>
		public bool IsCurrent();

		/// <summary>
		/// Get player by id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public IPlayer GetPlayer(int id);

		/// <summary>
		/// Get the entity by id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public IEntity GetEntity(int id);

		/// <summary>
		/// Get number of entities in the session.
		/// </summary>
		/// <returns></returns>
		public int GetEntityCount();

		/// <summary>
		/// Get the player by index.
		/// </summary>
		/// <returns></returns>
		public int GetPlayerCount();

		/// <summary>
		/// Close the session.
		/// </summary>
		/// <returns></returns>
		public UniTask Dispose();

		/// <summary>
		/// Notify the session that a player has joined.
		/// </summary>
		/// <param name="player"></param>
		void OnPlayerJoined(IPlayer player);

		/// <summary>
		/// Notify the session that a player has left.
		/// </summary>
		/// <param name="player"></param>
		void OnPlayerLeft(IPlayer player);

		/// <summary>
		/// Notify the session that a player has been selected as the current session.
		/// </summary>
		/// <param name="player"></param>
		void OnAuthorityTransferred(IPlayer player);

		/// <summary>
		/// Check if the session matches a specific world identifier.
		/// </summary>
		/// <param name="identifier"></param>
		/// <returns></returns>
		bool Match(IWorldIdentifier identifier);

		/// <summary>
		/// Notify the session that its state has changed.
		/// </summary>
		/// <param name="state"></param>
		/// <param name="previousState"></param>
		void OnStateChanged(IAdapterState state, IAdapterState previousState);
	}
}