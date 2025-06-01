namespace Nox.Entities {
	public interface IEntityManager {
		/// <summary>
		/// Register an entity with the manager.
		/// </summary>
		/// <param name="entity"></param>
		public void RegisterEntity(IEntity entity);

		/// <summary>
		/// Unregister an entity from the manager.
		/// </summary>
		/// <param name="entity"></param>
		public void UnregisterEntity(IEntity entity);

		/// <summary>
		/// Get an entity by its ID.
		/// </summary>
		/// <param name="id"></param>
		/// <returns>The entity with the specified ID, or null if not found.</returns>
		public IEntity GetEntity(int id);

		/// <summary>
		/// Get an entity by its ID and type.
		/// </summary>
		/// <param name="id"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T GetEntity<T>(int id) where T : IEntity;

		/// <summary>
		/// Get all entities managed by this manager.
		/// </summary>
		/// <returns></returns>
		public IEntity[] GetEntities();

		/// <summary>
		/// Get all entities of a specific type managed by this manager.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T[] GetEntities<T>() where T : IEntity;

		/// <summary>
		/// Check if an entity with the specified ID exists in the manager.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool HasEntity(int id);

		/// <summary>
		/// Check if an entity of a specific type with the specified ID exists in the manager.
		/// </summary>
		/// <param name="id"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public bool HasEntity<T>(int id) where T : IEntity;

		/// <summary>
		/// Get the count of all entities managed by this manager.
		/// </summary>
		/// <returns></returns>
		int GetCount();

		/// <summary>
		/// Get the count of entities of a specific type managed by this manager.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		int GetCount<T>() where T : IEntity;
	}
}