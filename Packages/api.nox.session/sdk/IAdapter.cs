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
		/// Get the first local player in the session.
		/// </summary>
		/// <returns></returns>
		public IPlayer GetLocalPlayer();

		/// <summary>
		/// Get the master player in the session, which is the player that has the authority over the session.
		/// </summary>
		/// <returns></returns>
		public IPlayer GetMasterPlayer();

		/// <summary>
		/// Allow to the master player to transfer the authority to another player.
		/// </summary>
		/// <param name="player"></param>
		/// <returns>Returns true if the authority was successfully transferred, false otherwise.</returns>
		public UniTask<bool> TransferAuthority(IPlayer player);

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
		public int GetEntityCount();

		/// <summary>
		/// Get the count of players in the session.
		/// </summary>
		/// <returns></returns>
		public int GetPlayerCount();

		/// <summary>
		/// Get the world associated with the session.
		/// </summary>
		/// <returns></returns>
		public IDimension GetCurrentDimension();

		/// <summary>
		/// Set the current dimension of the session.
		/// </summary>
		/// <param name="key"></param>
		void SetCurrentDimension(string key);

		/// <summary>
		/// Get all dimensions in the session.
		/// </summary>
		/// <returns></returns>
		public IDimension[] GetDimensions();

		/// <summary>
		/// Called when the session is deselected (is not the current session).
		/// </summary>
		/// <param name="newSession">The new session that is now current.</param>
		public UniTask OnDeselect(ISession newSession);

		/// <summary>
		/// Called when the session is selected (is the current session).
		/// </summary>
		/// <param name="oldSession">The previous session that was current.</param>
		public UniTask OnSelect(ISession oldSession);

		/// <summary>
		/// Add a dimension to the session.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="scene"></param>
		public void AddDimension(string key, IScene scene);

		/// <summary>
		/// Remove a dimension from the session.
		/// </summary>
		/// <param name="key"></param>
		public void RemoveDimension(string key);

		/// <summary>
		/// Get the current state of the adapter, which includes the current operation and its progress.
		/// (used when the session is loading (e.g. loading a world or connecting to a server)).
		/// If there is no operation, it returns an empty string and 1.0f (100%).
		/// If the adapter is errored, it returns the error message and -1.0f.
		/// </summary>
		/// <returns></returns>
		public IAdapterState GetState();
	}
}