using UnityEngine;

namespace Nox.CCK.Utils {
	public class Transform {
		public const float DefaultThreshold = 0.0001f;
		private Vector3        _position;
		private Quaternion     _rotation;
		private Vector3        _scale;
		private Vector3        _velocity;
		private Vector3        _angularVelocity;
		private TransformFlags _flags = TransformFlags.None;

		public TransformFlags Flags
			=> _flags;

		// POSITION

		/// <summary>
		/// Get the position of the transform.
		/// </summary>
		/// <returns></returns>
		public Vector3 GetPosition()
			=> _flags.HasFlag(TransformFlags.Position) ? _position : Vector3.zero;

		/// <summary>
		/// Set the position of the transform.
		/// </summary>
		/// <param name="value">Vector3 of the new position</param>
		public void SetPosition(Vector3 value) {
			_position =  value;
			_flags    |= TransformFlags.Position;
		}

		/// <summary>
		/// Reset the position of the transform.
		/// </summary>
		public void ResetPosition() 
			=> _flags    &= ~TransformFlags.Position;

		// ROTATION

		/// <summary>
		/// Get the rotation of the transform.
		/// </summary>
		/// <returns></returns>
		public Quaternion GetRotation()
			=> _flags.HasFlag(TransformFlags.Rotation) ? _rotation : Quaternion.identity;

		/// <summary>
		/// Set the rotation of the transform.
		/// </summary>
		/// <param name="value">Quaternion of the new rotation</param>
		public void SetRotation(Quaternion value) {
			_rotation =  value;
			_flags    |= TransformFlags.Rotation;
		}

		/// <summary>
		/// Reset the rotation of the transform.
		/// </summary>
		public void ResetRotation()
			=> _flags &= ~TransformFlags.Rotation;


		// SCALE

		/// <summary>
		/// Get the scale of the transform.
		/// </summary>
		/// <returns></returns>
		public Vector3 GetScale()
			=> _flags.HasFlag(TransformFlags.Scale) ? _scale : Vector3.one;

		/// <summary>
		/// Set the scale of the transform.
		/// </summary>
		/// <param name="value">Vector3 of the new scale</param>
		public void SetScale(Vector3 value) {
			_scale =  value;
			_flags |= TransformFlags.Scale;
		}

		/// <summary>
		/// Reset the scale of the transform.
		/// </summary>
		public void ResetScale()
			=> _flags &= ~TransformFlags.Scale;

		// VELOCITY

		/// <summary>
		/// Get the velocity of the transform.
		/// </summary>
		/// <returns></returns>
		public Vector3 GetVelocity()
			=> _flags.HasFlag(TransformFlags.Velocity) ? _velocity : Vector3.zero;

		/// <summary>
		/// Set the velocity of the transform.
		/// </summary>
		/// <param name="value">Vector3 of the new velocity</param>
		public void SetVelocity(Vector3 value) {
			_velocity =  value;
			_flags    |= TransformFlags.Velocity;
		}

		/// <summary>
		/// Reset the velocity of the transform.
		/// </summary>
		public void ResetVelocity()
			=> _flags &= ~TransformFlags.Velocity;


		// ANGULAR VELOCITY

		/// <summary>
		/// Get the angular velocity of the transform.
		/// </summary>
		/// <returns></returns>
		public Vector3 GetAngularVelocity()
			=> _flags.HasFlag(TransformFlags.AngularVelocity) ? _angularVelocity : Vector3.zero;

		/// <summary>
		/// Set the angular velocity of the transform.
		/// </summary>
		/// <param name="value">Vector3 of the new angular velocity</param>
		public void SetAngularVelocity(Vector3 value) {
			_angularVelocity =  value;
			_flags           |= TransformFlags.AngularVelocity;
		}

		/// <summary>
		/// Reset the angular velocity of the transform.
		/// </summary>
		public void ResetAngularVelocity()
			=> _flags &= ~TransformFlags.AngularVelocity;

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
		public bool Equals(Transform transform, float threshold = DefaultThreshold)
			=> _flags == transform._flags
				&& IsSamePosition(transform._position, threshold)
				&& IsSameRotation(transform._rotation, threshold)
				&& IsSameScale(transform._scale, threshold)
				&& IsSameVelocity(transform._velocity, threshold)
				&& IsSameAngularVelocity(transform._angularVelocity, threshold);

		public bool IsSameScale(Vector3 value, float threshold = DefaultThreshold)
			=> _flags.HasFlag(TransformFlags.Scale) && Vector3.Distance(_scale, value) < threshold;

		public bool IsSamePosition(Vector3 value, float threshold = DefaultThreshold)
			=> _flags.HasFlag(TransformFlags.Position) && Vector3.Distance(_position, value) < threshold;

		public bool IsSameRotation(Quaternion value, float threshold = DefaultThreshold)
			=> _flags.HasFlag(TransformFlags.Rotation) && Quaternion.Angle(_rotation, value) < threshold;

		public bool IsSameVelocity(Vector3 value, float threshold = DefaultThreshold)
			=> _flags.HasFlag(TransformFlags.Velocity) && Vector3.Distance(_velocity, value) < threshold;

		public bool IsSameAngularVelocity(Vector3 value, float threshold = DefaultThreshold)
			=> _flags.HasFlag(TransformFlags.AngularVelocity) && Vector3.Distance(_angularVelocity, value) < threshold;

		public override string ToString()
			=> $"{GetType().Name}[Flags={_flags}, Position={GetPosition()}, Rotation={GetRotation()}, Scale={GetScale()}, Velocity={GetVelocity()}, AngularVelocity={GetAngularVelocity()}]";
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