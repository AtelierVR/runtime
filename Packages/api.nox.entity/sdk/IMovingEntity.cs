using Nox.CCK.Network;
using UnityEngine;
using Transform = Nox.CCK.Utils.Transform;

namespace Nox.Entities {
	/// <summary>
	/// Interface for moving entities.
	/// </summary>
	public interface IMovingEntity : IEntity {
		/// <summary>
		/// Get the position of the entity.
		/// </summary>
		/// <returns></returns>
		public Vector3 GetPosition();

		/// <summary>
		/// Set the position of the entity.
		/// </summary>
		/// <param name="position"></param>
		/// <param name="markDirty"></param>
		public void SetPosition(Vector3 position, DirtyBy markDirty);

		/// <summary>
		/// Get the velocity of the entity.
		/// </summary>
		/// <returns></returns>
		public Vector3 GetVelocity();

		/// <summary>
		/// Set the velocity of the entity.
		/// </summary>
		/// <param name="velocity"></param>
		/// <param name="markDirty"></param>
		public void SetVelocity(Vector3 velocity, DirtyBy markDirty);

		/// <summary>
		/// Get the rotation of the entity.
		/// </summary>
		/// <returns></returns>
		public Quaternion GetRotation();

		/// <summary>
		/// Set the rotation of the entity.
		/// </summary>
		/// <param name="rotation"></param>
		/// <param name="markDirty"></param>
		public void SetRotation(Quaternion rotation, DirtyBy markDirty);

		/// <summary>
		/// Get the angular velocity of the entity.
		/// </summary>
		/// <returns></returns>
		public Vector3 GetAngularVelocity();

		/// <summary>
		/// Set the angular velocity of the entity.
		/// </summary>
		/// <param name="angularVelocity"></param>
		/// <param name="markDirty"></param>
		public void SetAngularVelocity(Vector3 angularVelocity, DirtyBy markDirty);
	}
}