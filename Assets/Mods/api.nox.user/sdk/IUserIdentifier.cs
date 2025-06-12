namespace Nox.Users {
	public interface IUserIdentifier {
		/// <summary>
		/// Indicates if the identifier is valid.
		/// </summary>
		/// <returns></returns>
		public bool IsValid();

		/// <summary>
		/// Indicates if the identifier have a server associated with this identifier.
		/// </summary>
		/// <returns></returns>
		public bool IsLocal();

		/// <summary>
		/// Indicates if the identifier is an ID.
		/// </summary>
		public bool IsId();

		/// <summary>
		/// Indicates if the identifier is a username.
		/// </summary>
		/// <returns></returns>
		public bool IsUsername();

		/// <summary>
		/// Gets the identifier as a string.
		/// if the identifier is not valid, it returns an empty string.
		/// </summary>
		/// <returns></returns>
		public string GetUsername();

		/// <summary>
		/// Gets the identifier as a uint.
		/// If the identifier is not valid, it returns 0.
		/// </summary>
		/// <returns></returns>
		public uint GetId();

		/// <summary>
		/// Tries to get the identifier as id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool TryGetId(out uint id);

		/// <summary>
		/// Tries to get the identifier as username.
		/// </summary>
		/// <param name="username"></param>
		/// <returns></returns>
		public bool TryGetUsername(out string username);

		/// <summary>
		/// Converts the identifier to a string.
		/// </summary>
		/// <param name="fallbackServer"></param>
		/// <returns></returns>
		public string ToString(string fallbackServer = null);

		/// <summary>
		/// Gets the server address associated with this identifier.
		/// </summary>
		/// <returns></returns>
		public string GetServerAddress();
	}
}