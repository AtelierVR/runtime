using System.Collections.Generic;

namespace Nox.Worlds {
	/// <summary>
	/// Represents a world identifier.
	/// <example>1564?v=latest&amp;dev@nox.org</example>
	/// </summary>
	public interface IWorldIdentifier {
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
		public uint GetId();

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
		/// The metadata is to help to identify a world or associated assets.
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, string[]> GetMetadata();

		/// <summary>
		/// Gets the version of the asset in the metadata.
		/// If the version is not set, it will return <see cref="ushort.MaxValue"/>.
		/// </summary>
		/// <returns></returns>
		public ushort GetVersion();

		/// <summary>
		/// Sets the version of the asset in the metadata.
		/// </summary>
		/// <param name="version"></param>
		public void SetVersion(ushort version);
	}
}