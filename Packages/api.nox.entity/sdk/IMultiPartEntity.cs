namespace Nox.Entities {
	/// <summary>
	/// An entity composed of multiple parts.
	/// </summary>
	public interface IMultiPartEntity : IEntity {
		/// <summary>
		/// Get all parts of the entity.
		/// </summary>
		/// <returns></returns>
		public IPart[] GetParts();

		/// <summary>
		/// Try to get a part by name.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="part"></param>
		/// <returns></returns>
		public bool TryGetPart(ushort name, out IPart part);
	}
}