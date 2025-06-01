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
	}
}