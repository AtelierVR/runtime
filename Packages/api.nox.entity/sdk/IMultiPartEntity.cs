using System.Collections;

namespace Nox.Entities {
	/// <summary>
	/// An entity composed of multiple parts.
	/// </summary>
	public interface IMultiPartEntity<TPart> : IEntity
		where TPart : IPart {
		/// <summary>
		/// Get all parts of the entity.
		/// </summary>
		/// <returns></returns>
		public TPart[] GetParts();

		/// <summary>
		/// Try to get a part by name.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="part"></param>
		/// <returns></returns>
		public bool TryGetPart(ushort name, out TPart part);
	}
}