namespace Nox.Entities {
	/// <summary>
	/// Interface for the entity API.
	/// </summary>
	public interface IEntityAPI {
		/// <summary>
		/// Create a new entity manager.
		/// </summary>
		/// <returns></returns>
		public IEntities New();
	}
}