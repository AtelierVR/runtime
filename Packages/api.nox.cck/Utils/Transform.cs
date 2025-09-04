using UnityEngine;

namespace Nox.CCK.Utils {
	public class Transform {
		private Vector3    position;
		private Quaternion rotation;
		private Vector3    scale;
		private Vector3    velocity;
		private Vector3    angularVelocity;

		public DeliveryType DeliveryType = DeliveryType.None;
		public TransformFlags        Flags { get; private set; } = TransformFlags.None;

		// POSITION

		/// <summary>
		/// Get the position of the transform.
		/// </summary>
		/// <returns></returns>
		public Vector3 GetPosition()
			=> Flags.HasFlag(TransformFlags.Position) ? position : Vector3.zero;

		/// <summary>
		/// Set the position of the transform.
		/// </summary>
		/// <param name="value">Vector3 of the new position</param>
		public void SetPosition(Vector3 value) {
			position =  value;
			Flags    |= TransformFlags.Position;
		}

		/// <summary>
		/// Reset the position of the transform.
		/// </summary>
		public void ResetPosition() {
			position =  Vector3.zero;
			Flags    &= ~TransformFlags.Position;
		}

		// ROTATION

		/// <summary>
		/// Get the rotation of the transform.
		/// </summary>
		/// <returns></returns>
		public Quaternion GetRotation()
			=> Flags.HasFlag(TransformFlags.Rotation) ? rotation : Quaternion.identity;

		/// <summary>
		/// Set the rotation of the transform.
		/// </summary>
		/// <param name="value">Quaternion of the new rotation</param>
		public void SetRotation(Quaternion value) {
			rotation =  value;
			Flags    |= TransformFlags.Rotation;
		}

		/// <summary>
		/// Reset the rotation of the transform.
		/// </summary>
		public void ResetRotation() {
			rotation =  Quaternion.identity;
			Flags    &= ~TransformFlags.Rotation;
		}

		// SCALE

		/// <summary>
		/// Get the scale of the transform.
		/// </summary>
		/// <returns></returns>
		public Vector3 GetScale()
			=> Flags.HasFlag(TransformFlags.Scale) ? scale : Vector3.one;

		/// <summary>
		/// Set the scale of the transform.
		/// </summary>
		/// <param name="value">Vector3 of the new scale</param>
		public void SetScale(Vector3 value) {
			scale =  value;
			Flags |= TransformFlags.Scale;
		}

		/// <summary>
		/// Reset the scale of the transform.
		/// </summary>
		public void ResetScale() {
			scale =  Vector3.one;
			Flags &= ~TransformFlags.Scale;
		}

		// VELOCITY

		/// <summary>
		/// Get the velocity of the transform.
		/// </summary>
		/// <returns></returns>
		public Vector3 GetVelocity()
			=> Flags.HasFlag(TransformFlags.Velocity) ? velocity : Vector3.zero;

		/// <summary>
		/// Set the velocity of the transform.
		/// </summary>
		/// <param name="value">Vector3 of the new velocity</param>
		public void SetVelocity(Vector3 value) {
			velocity =  value;
			Flags    |= TransformFlags.Velocity;
		}

		/// <summary>
		/// Reset the velocity of the transform.
		/// </summary>
		public void ResetVelocity() {
			velocity =  Vector3.zero;
			Flags    &= ~TransformFlags.Velocity;
		}

		// ANGULAR VELOCITY

		/// <summary>
		/// Get the angular velocity of the transform.
		/// </summary>
		/// <returns></returns>
		public Vector3 GetAngularVelocity()
			=> Flags.HasFlag(TransformFlags.AngularVelocity) ? angularVelocity : Vector3.zero;

		/// <summary>
		/// Set the angular velocity of the transform.
		/// </summary>
		/// <param name="value">Vector3 of the new angular velocity</param>
		public void SetAngularVelocity(Vector3 value) {
			angularVelocity =  value;
			Flags           |= TransformFlags.AngularVelocity;
		}

		/// <summary>
		/// Reset the angular velocity of the transform.
		/// </summary>
		public void ResetAngularVelocity() {
			angularVelocity =  Vector3.zero;
			Flags           &= ~TransformFlags.AngularVelocity;
		}

		/// <summary>
		/// Create a new empty transform.
		/// </summary>
		public Transform() { }

		/// <summary>
		/// Create a new transform with a position, rotation, scale, velocity and angular velocity.
		/// </summary>
		/// <param name="transform">Transform of a gameobject</param>
		/// <param name="rigidbody">Rigidbody of a gameobject</param>
		public Transform(UnityEngine.Transform transform, Rigidbody rigidbody = null) {
			SetPosition(transform.position);
			SetRotation(transform.rotation);
			SetScale(transform.localScale);
			if (!rigidbody) return;
			SetVelocity(rigidbody.linearVelocity);
			SetAngularVelocity(rigidbody.angularVelocity);
		}

		/// <summary>
		/// Check if the transform is equal to another transform with a threshold.
		/// </summary>
		/// <param name="transform">Transform to compare</param>
		/// <param name="threshold">Threshold for the comparison (optional)</param>
		/// <returns>True if the transform is equal</returns>
		public bool Equals(Transform transform, float threshold = float.Epsilon)
			=> Flags == transform.Flags
				&& IsSamePosition(transform.position, threshold)
				&& IsSameRotation(transform.rotation, threshold)
				&& IsSameScale(transform.scale, threshold)
				&& IsSameVelocity(transform.velocity, threshold)
				&& IsSameAngularVelocity(transform.angularVelocity, threshold);

		public bool IsSameScale(Vector3 value, float threshold = float.Epsilon)
			=> Flags.HasFlag(TransformFlags.Scale) && Vector3.Distance(scale, value) < threshold;

		public bool IsSamePosition(Vector3 value, float threshold = float.Epsilon)
			=> Flags.HasFlag(TransformFlags.Position) && Vector3.Distance(position, value) < threshold;

		public bool IsSameRotation(Quaternion value, float threshold = float.Epsilon)
			=> Flags.HasFlag(TransformFlags.Rotation) && Quaternion.Angle(rotation, value) < threshold;

		public bool IsSameVelocity(Vector3 value, float threshold = float.Epsilon)
			=> Flags.HasFlag(TransformFlags.Velocity) && Vector3.Distance(velocity, value) < threshold;

		public bool IsSameAngularVelocity(Vector3 value, float threshold = float.Epsilon)
			=> Flags.HasFlag(TransformFlags.AngularVelocity) && Vector3.Distance(angularVelocity, value) < threshold;
	}


	public enum DeliveryType {
		None,
		LocalModified,
		RemoteModified
	}

	[System.Flags]
	public enum TransformFlags : byte {
		None            = 0,
		Position        = 1 << 0,
		Rotation        = 1 << 1,
		Scale           = 1 << 2,
		Velocity        = 1 << 3,
		AngularVelocity = 1 << 4,
		Reset           = 1 << 5,
		All             = Rigidbody | Transform,
		Rigidbody       = Velocity  | AngularVelocity,
		Transform       = Position  | Rotation | Scale
	}
}