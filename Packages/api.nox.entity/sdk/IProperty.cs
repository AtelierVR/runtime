using System;
using Nox.CCK.Network;

namespace Nox.Entities {
	/// <summary>
	/// Interface for a property of an entity.
	/// </summary>
	public interface IProperty : IDirty, ISerializable {
		/// <summary>
		/// Get the key of the property.
		/// Is normally CRC32 hash of the name.
		/// </summary>
		/// <returns></returns>
		public int Key { get; }

		/// <summary>
		/// Get the last updated time of the property.
		/// </summary>
		/// <returns></returns>
		public DateTime UpdatedAt { get; }

		/// <summary>
		/// Get the name of the property.
		/// (optional, may be null)
		/// </summary>
		/// <returns></returns>
		public string Name { get; }

		/// <summary>
		/// Get the value of the property.
		/// </summary>
		/// <returns></returns>
		public object Value { get; set; }

		/// <summary>
		/// Get the flags of the property.
		/// </summary>
		public PropertyFlags Flags { get; }
	}
}