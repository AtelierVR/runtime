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
	}
}