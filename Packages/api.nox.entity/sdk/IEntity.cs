namespace Nox.Entities {
	public interface IEntity {
		/// <summary>
		/// Get the unique identifier of the entity.
		/// </summary>
		/// <returns></returns>
		public int Id { get; }
		
		/// <summary>
		/// Get all properties of the entity.
		/// </summary>
		/// <returns></returns>
		public IProperty[] GetProperties();

		/// <summary>
		/// Get a property of the entity by key.
		/// </summary>
		public bool TryGetProperty(int key, out IProperty property);

		/// <summary>
		/// Check if the entity has a physical component.
		/// </summary>
		/// <returns></returns>
		public bool HasPhysical();

		/// <summary>
		/// Try to get the physical component of the entity.
		/// </summary>
		/// <param name="physical"></param>
		/// <returns></returns>
		public bool TryGetPhysical<T>(out T physical) where T : Physical;

		/// <summary>
		/// Create a new physical component for the entity.
		/// (only one physical component can exist at a time for an entity)
		/// </summary>
		/// <returns></returns>
		public bool MakePhysical();

		/// <summary>
		/// Destroy the physical component of the entity.
		/// </summary>
		public void DestroyPhysical();
	}
}