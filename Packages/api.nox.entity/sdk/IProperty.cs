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
		public int GetKey();
		
		/// <summary>
		/// Get the name of the property.
		/// (optional, may be null)
		/// </summary>
		/// <returns></returns>
		public string GetName();

		/// <summary>
		/// Get the value of the property.
		/// </summary>
		/// <returns></returns>
		public object GetValue();

		public PropertyFlags GetFlags();

		/// <summary>
		/// Set the value of the property.
		/// You can be sure that the type of the value matches the type of the property.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="by"></param>
		public void SetValue(object value, DirtyBy by);
	}
}