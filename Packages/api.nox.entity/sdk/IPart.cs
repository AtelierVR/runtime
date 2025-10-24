using Nox.CCK.Network;
using UnityEngine;

namespace Nox.Entities {
	/// <summary>
	/// Represents a part of a multi-part entity.
	/// </summary>
	public interface IPart : IDirty {
		/// <summary>
		/// Gets the unique ID of this part within its parent entity.
		/// </summary>
		/// <returns></returns>
		public ushort GetId();

		/// <summary>
		/// Get the position of this part in world space.
		/// </summary>
		/// <returns></returns>
		public bool TryGetPosition(out Vector3 position);

		/// <summary>
		/// Get the rotation of this part in world space.
		/// </summary>
		/// <returns></returns>
		public bool TryGetRotation(out Quaternion rotation);

		/// <summary>
		/// Get the scale of this part in world space.
		/// </summary>
		/// <returns></returns>
		public bool TryGetScale(out Vector3 scale);

		/// <summary>
		/// Get the angular velocity of this part in world space.
		/// </summary>
		/// <returns></returns>
		public bool TryGetAngularVelocity(out Vector3 angularVelocity);

		/// <summary>
		/// Get the velocity of this part in world space.
		/// </summary>
		/// <returns></returns>
		public bool TryGetVelocity(out Vector3 transform);

		/// <summary>
		/// Set the position of this part in world space.
		/// </summary>
		/// <param name="position"></param>
		/// <param name="markDirty"></param>
		public void SetPosition(Vector3 position, bool markDirty = true);

		/// <summary>
		/// Set the rotation of this part in world space.
		/// </summary>
		/// <param name="rotation"></param>
		/// <param name="markDirty"></param>
		public void SetRotation(Quaternion rotation, bool markDirty = true);

		/// <summary>
		/// Set the scale of this part in world space.
		/// </summary>
		/// <param name="scale"></param>
		/// <param name="markDirty"></param>
		public void SetScale(Vector3 scale, bool markDirty = true);

		/// <summary>
		/// Set the velocity of this part in world space.
		/// </summary>
		/// <param name="velocity"></param>
		/// <param name="markDirty"></param>
		public void SetVelocity(Vector3 velocity, bool markDirty = true);

		/// <summary>
		/// Set the angular velocity of this part in world space.
		/// </summary>
		/// <param name="angularVelocity"></param>
		/// <param name="markDirty"></param>
		public void SetAngularVelocity(Vector3 angularVelocity, bool markDirty = true);
	}
}