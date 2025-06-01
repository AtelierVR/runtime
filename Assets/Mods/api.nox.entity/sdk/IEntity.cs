using System.Collections.Generic;
using System.Numerics;

namespace Nox.Entities {
	public interface IEntity {
		/// <summary>
		/// Get the unique identifier of the entity.
		/// </summary>
		/// <returns></returns>
		public int GetIndex();

		/// <summary>
		/// Get all properties of the entity.
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, object> GetProperties();

		/// <summary>
		/// Get a property of the entity by key.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T GetProperty<T>(string key, T defaultValue) where T : struct;

		/// <summary>
		/// Set a property of the entity by key.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <typeparam name="T"></typeparam>
		public void SetProperty<T>(string key, T value) where T : struct;

		/// <summary>
		/// Remove a property of the entity by key.
		/// </summary>
		/// <param name="key"></param>
		public void RemoveProperty(string key);

		/// <summary>
		/// Get the position of the entity.
		/// </summary>
		/// <returns></returns>
		public Vector3 GetPosition();

		/// <summary>
		/// Get the rotation of the entity.
		/// </summary>
		/// <returns></returns>
		public Quaternion GetRotation();

		/// <summary>
		/// Set the position and rotation of the entity.
		/// </summary>
		/// <param name="position"></param>
		public void SetPosition(Vector3 position);

		/// <summary>
		/// Set the rotation of the entity.
		/// </summary>
		/// <param name="rotation"></param>
		public void SetRotation(Quaternion rotation);

		/// <summary>
		/// Try to get the physical component of the entity.
		/// </summary>
		/// <param name="physical"></param>
		/// <returns></returns>
		public bool TryGetPhysical(out Physical physical);
	}
}