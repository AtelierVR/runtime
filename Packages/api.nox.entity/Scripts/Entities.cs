using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Utils;
using Nox.Entities;
using UnityEngine.Events;

namespace api.nox.entity {
	public class Entities : IEntities, INoxObject {
		private readonly List<IEntity> _entities = new();

		public T[] GetEntities<T>() where T : IEntity
			=> _entities
				.Where(entity => entity is T)
				.Cast<T>()
				.ToArray();

		public bool HasEntity(int id)
			=> _entities.Any(entity => entity.Id == id);

		public bool HasEntity<T>(int id) where T : IEntity
			=> _entities.Any(entity => entity.Id == id && entity is T);

		public int GetCount()
			=> _entities.Count;

		public int GetCount<T>() where T : IEntity
			=> _entities.Count(entity => entity is T);

		public UnityEvent<IEntity> OnEntityAdded { get; } = new();
		public UnityEvent<IEntity> OnEntityRemoved { get; } = new();

		public void RegisterEntity(IEntity entity) {
			if (entity == null) {
				Logger.LogError("Cannot register a null entity.");
				return;
			}

			if (HasEntity(entity.Id)) {
				Logger.LogWarning($"Entity with ID {entity.Id} is already registered.");
				return;
			}

			_entities.Add(entity);
			OnEntityAdded.Invoke(entity);
			Logger.Log($"Registered entity with ID {entity.Id}.");
		}

		public void UnregisterEntity(IEntity entity) {
			if (entity == null) {
				Logger.LogError("Cannot unregister a null entity.");
				return;
			}

			if (!HasEntity(entity.Id)) {
				Logger.LogWarning($"Entity with ID {entity.Id} is not registered.");
				return;
			}

			_entities.Remove(entity);
			OnEntityRemoved.Invoke(entity);
			Logger.Log($"Unregistered entity with ID {entity.Id}.");
		}

		public IEntity GetEntity(int id)
			=> _entities.FirstOrDefault(entity => entity.Id == id);

		public T GetEntity<T>(int id) where T : IEntity
			=> (T)_entities.FirstOrDefault(entity => entity.Id == id && entity is T);

		public IEntity[] GetEntities()
			=> _entities.ToArray();

		public override string ToString()
			=> $"{GetType().Name}[Count={_entities.Count}]";
	}
}