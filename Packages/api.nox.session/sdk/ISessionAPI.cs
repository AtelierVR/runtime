using System.Collections.Generic;

namespace Nox.Sessions {
	/// <summary>
	/// Interface for the session API, allowing to manage sessions.
	/// </summary>
	public interface ISessionAPI {
		/// <summary>
		/// Create a new session with the given adapter.
		/// </summary>
		/// <param name="adapter"></param>
		/// <returns></returns>
		public ISession New(IAdapter adapter);

		/// <summary>
		/// Get a session by its ID.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public ISession GetSession(ushort id);

		/// <summary>
		/// Get all sessions currently managed by the session API.
		/// </summary>
		/// <returns></returns>
		public ISession[] GetSessions();

		/// <summary>
		/// Get the count of sessions currently managed by the session API.
		/// </summary>
		/// <returns></returns>
		public int GetSessionCount();

		/// <summary>
		/// Request if is possible to make a session with the given adapter.
		/// </summary>
		/// <param name="adapter"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		bool CanMakeSession(string adapter, Dictionary<string, object> options = null);

		/// <summary>
		/// Make a session with the given adapter.
		/// </summary>
		/// <param name="adapter"></param>
		/// <param name="options">Options for the session, please refer to the adapter documentation for available options.</param>
		/// <returns>If the session is successfully created, it will return the session instance, otherwise it will return null.</returns>
		public ISession MakeSession(string adapter, Dictionary<string, object> options = null);

		/// <summary>
		/// Get the current session, if any.
		/// </summary>
		/// <returns></returns>
		public ISession GetCurrent();
	}
}