using System.Collections.Generic;

namespace Nox.Instances {
	public interface IInstanceIdentifier {
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
		/// Indicates if the identifier is a name.
		/// </summary>
		/// <returns></returns>
		public bool IsName();

		/// <summary>
		/// Gets the identifier as a string.
		/// if the identifier is not valid, it returns an empty string.
		/// </summary>
		/// <returns></returns>
		public string GetName();

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
		/// Tries to get the identifier as name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool TryGetName(out string name);

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

		/// <summary>
		/// Gets the metadata associated with this identifier.
		/// The metadata is to help to identify a instance with specific properties.
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, string[]> GetMetadata();

		/// <summary>
		/// Checks if this identifier is equal to another identifier.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(IInstanceIdentifier other);

		/// <summary>
		/// Checks if this identifier is equal to another string identifier.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(string other);
	}
}