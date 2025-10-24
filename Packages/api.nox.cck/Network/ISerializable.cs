namespace Nox.CCK.Network {
	public interface ISerializable {
		/// <summary>
		/// Serialize the object to a byte array.
		/// </summary>
		/// <returns></returns>
		public byte[] Serialize();

		/// <summary>
		/// Deserialize the object from a byte array.
		/// </summary>
		/// <param name="data"></param>
		public void Deserialize(byte[] data);
	}
}