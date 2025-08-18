using System.Collections.Generic;
using UnityEngine;

namespace Nox.Entities {
	public interface IEntity {
		/// <summary>
		/// Get the unique identifier of the entity.
		/// </summary>
		/// <returns></returns>
		public int GetId();

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
		/// Get the velocity of the entity.
		/// </summary>
		/// <returns></returns>
		public Vector3 GetVelocity();

		/// <summary>
		/// Set the velocity of the entity.
		/// </summary>
		/// <param name="velocity"></param>
		public void SetVelocity(Vector3 velocity);

		/// <summary>
		/// Get the angular velocity of the entity.
		/// </summary>
		/// <returns></returns>
		public Vector3 GetAngularVelocity();

		/// <summary>
		/// Set the angular velocity of the entity.
		/// </summary>
		/// <param name="angular"></param>
		public void SetAngularVelocity(Vector3 angular);

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