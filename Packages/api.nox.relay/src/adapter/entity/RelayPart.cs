using Nox.Avatars.Rigging;
using Nox.CCK.Utils;
using Nox.Entities;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace api.nox.relay {
	/// <summary>
	/// Represents a part of a <see cref="RelayEntity"/>.
	/// </summary>
	public class RelayPart : Nox.CCK.Utils.Transform, IPart {
		private readonly RelayEntity _entity;
		private readonly IRigPart    _part;

		/// <summary>
		/// Initializes a new instance of the <see cref="RelayPart"/> class.
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="part"></param>
		public RelayPart(RelayEntity entity, IRigPart part) {
			_entity = entity;
			_part   = part;
		}

		/// <summary>
		/// Tries to get the GameObject associated with this part.
		/// </summary>
		/// <param name="go"></param>
		/// <param name="rigid"></param>
		/// <returns></returns>
		private bool TryGet(out Transform go, out Rigidbody rigid) {
			if (_part == null) {
				go    = null;
				rigid = null;
				return false;
			}

			_part.TryGetTransform(out go, out rigid);
			return go;
		}

		/// <summary>
		/// Get the ID of the part.
		/// </summary>
		/// <returns></returns>
		public ushort GetId()
			=> _part.GetId();

		/// <summary>
		/// Try to get the position of the part.
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public bool TryGetPosition(out Vector3 position) {
			if (!TryGet(out var transform, out _)) {
				position = GetPosition();
				return Flags.HasFlag(TransformFlags.Position);
			}

			position = transform.position;
			return true;
		}

		/// <summary>
		/// Try to get the rotation of the part.
		/// </summary>
		/// <param name="rotation"></param>
		/// <returns></returns>
		public bool TryGetRotation(out Quaternion rotation) {
			if (!TryGet(out var transform, out _)) {
				rotation = GetRotation();
				return Flags.HasFlag(TransformFlags.Rotation);
			}

			rotation = transform.rotation;
			return true;
		}

		/// <summary>
		/// Try to get the scale of the part.
		/// </summary>
		public bool TryGetScale(out Vector3 scale) {
			if (!TryGet(out var transform, out _)) {
				scale = GetScale();
				return Flags.HasFlag(TransformFlags.Scale);
			}

			scale = transform.localScale;
			return true;
		}

		/// <summary>
		/// Try to get the velocity of the part.
		/// </summary>
		public bool TryGetVelocity(out Vector3 velocity) {
			if (!TryGet(out _, out var rigid) || !rigid) {
				velocity = GetVelocity();
				return Flags.HasFlag(TransformFlags.Velocity);
			}

			velocity = rigid.linearVelocity;
			return true;
		}

		/// <summary>
		/// Try to get the angular velocity of the part.
		/// </summary>
		public bool TryGetAngularVelocity(out Vector3 angularVelocity) {
			if (!TryGet(out _, out var rigid) || !rigid) {
				angularVelocity = GetAngularVelocity();
				return Flags.HasFlag(TransformFlags.AngularVelocity);
			}

			angularVelocity = rigid.angularVelocity;
			return true;
		}


		void IPart.SetPosition(Vector3 position, bool markDirty) {
			if (TryGet(out var transform, out _))
				transform.position = position;
			SetPosition(position);
		}

		void IPart.SetRotation(Quaternion rotation, bool markDirty) {
			if (TryGet(out var transform, out _)) 
				transform.rotation = rotation;
			SetRotation(rotation);
		}

		void IPart.SetScale(Vector3 scale, bool markDirty) {
			if (TryGet(out var transform, out _)) 
				transform.localScale = scale;
			SetScale(scale);
		}

		void IPart.SetVelocity(Vector3 velocity, bool markDirty) {
			if (TryGet(out _, out var rigid) && rigid) 
				rigid.linearVelocity = velocity;
			SetVelocity(velocity);
		}

		void IPart.SetAngularVelocity(Vector3 angularVelocity, bool markDirty) {
			if (TryGet(out _, out var rigid) && rigid) 
				rigid.angularVelocity = angularVelocity;
			SetAngularVelocity(angularVelocity);
		}


		public bool IsDirty()
			=> TryGetPosition(out var p)            && !IsSamePosition(p)
				|| TryGetRotation(out var r)        && !IsSameRotation(r)
				|| TryGetScale(out var s)           && !IsSameScale(s)
				|| TryGetVelocity(out var v)        && !IsSameVelocity(v)
				|| TryGetAngularVelocity(out var a) && !IsSameAngularVelocity(a);

		public void SetDirty(bool dirty = true) {
			if (dirty) return;
			ResetPosition();
			ResetRotation();
			ResetScale();
			ResetVelocity();
			ResetAngularVelocity();
		}

		public void LerpTarget(float time) {
			if (!TryGet(out var transform, out _) || !transform)
				return;

			transform.GetPositionAndRotation(out var position, out var rotation);

			if (!IsSamePosition(position)) {
				var p = GetPosition();
				position = Vector3.Distance(position, p) switch {
					> 5f     => p,
					<= 0.01f => p,
					_        => Vector3.Lerp(position, p, time / Vector3.Distance(position, p))
				};
			}

			if (!IsSameRotation(rotation)) {
				var r = GetRotation();
				rotation = Quaternion.Angle(rotation, r) switch {
					> 45f   => r,
					<= 0.1f => r,
					_       => Quaternion.Slerp(rotation, r, time / Quaternion.Angle(rotation, r))
				};
			}

			transform.SetPositionAndRotation(position, rotation);
		}
	}
}