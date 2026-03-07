using UnityEngine.Events;
namespace Nox.Entities {
	public interface IEntities : IEntities<IEntity> { }

	public interface IEntities<TEntity>
		where TEntity : IEntity {
		
		/// <summary>
		/// Get an entity by its ID.
		/// </summary>
		/// <param name="id"></param>
		/// <returns>The entity with the specified ID, or null if not found.</returns>
		public TEntity GetEntity(int id);

		/// <summary>
		/// Get an entity by its ID and type.
		/// </summary>
		/// <param name="id"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T GetEntity<T>(int id) where T : TEntity;

		/// <summary>
		/// Get all entities managed by this manager.
		/// </summary>
		/// <returns></returns>
		public TEntity[] GetEntities();

		/// <summary>
		/// Get all entities of a specific type managed by this manager.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T[] GetEntities<T>() where T : TEntity;

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
		public bool HasEntity<T>(int id) where T : TEntity;

		/// <summary>
		/// Get the count of all entities managed by this manager.
		/// </summary>
		/// <returns></returns>
		public int GetCount();

		/// <summary>
		/// Get the count of entities of a specific type managed by this manager.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public int GetCount<T>() where T : TEntity;
		
		/// <summary>
		/// Invoked when an entity is added to the manager.
		/// </summary>
		UnityEvent<TEntity> OnEntityAdded { get; }
		
		/// <summary>
		/// Invoked when an entity is removed from the manager.
		/// </summary>
		UnityEvent<TEntity> OnEntityRemoved { get; }
	}
}