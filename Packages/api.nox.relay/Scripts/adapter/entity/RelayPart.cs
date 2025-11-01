using System;
using Nox.CCK.Network;
using Nox.CCK.Utils;
using Nox.Entities;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Transform = Nox.CCK.Utils.Transform;
using UTransform = UnityEngine.Transform;

namespace api.nox.relay {
	/// <summary>
	/// Represents a part of a <see cref="RelayEntity"/>.
	/// </summary>
	public abstract class RelayPart : Transform, IPart {
		protected readonly RelayEntity Entity;
		private            DirtyBy     _dirty;

		protected RelayPart(RelayEntity entity)
			=> Entity = entity;

		/// <summary>
		/// Tries to get the GameObject associated by the implementation.
		/// </summary>
		/// <param name="go"></param>
		/// <param name="rigid"></param>
		/// <returns></returns>
		protected virtual bool TryGet(out UTransform go, out Rigidbody rigid) {
			go    = null;
			rigid = null;
			return false;
		}

		/// <summary>
		/// Get the ID.
		/// </summary>
		/// <returns></returns>
		public abstract ushort GetId();

		/// <summary>
		/// Try to get the position by the implementation.
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public bool TryGetPosition(out Vector3 position) {
			position = GetPosition();
			return Flags.HasFlag(TransformFlags.Position);
		}

		/// <summary>
		/// Try to get the rotation by the implementation.
		/// </summary>
		/// <param name="rotation"></param>
		/// <returns></returns>
		public bool TryGetRotation(out Quaternion rotation) {
			rotation = GetRotation();
			return Flags.HasFlag(TransformFlags.Rotation);
		}

		/// <summary>
		/// Try to get the scale by the implementation.
		/// </summary>
		public bool TryGetScale(out Vector3 scale) {
			scale = GetScale();
			return Flags.HasFlag(TransformFlags.Scale);
		}

		/// <summary>
		/// Try to get the velocity by the implementation.
		/// </summary>
		public bool TryGetVelocity(out Vector3 velocity) {
			velocity = GetVelocity();
			return Flags.HasFlag(TransformFlags.Velocity);
		}

		/// <summary>
		/// Try to get the angular velocity by the implementation.
		/// </summary>
		public bool TryGetAngularVelocity(out Vector3 angularVelocity) {
			angularVelocity = GetAngularVelocity();
			return Flags.HasFlag(TransformFlags.AngularVelocity);
		}

		void IPart.SetPosition(Vector3 position, DirtyBy markDirty) {
			if (Vector3.Distance(position, GetPosition()) < DefaultThreshold) return;
			SetPosition(position);
			SetDirty(markDirty);
		}

		void IPart.SetRotation(Quaternion rotation, DirtyBy markDirty) {
			if (Quaternion.Angle(rotation, GetRotation()) < DefaultThreshold) return;
			SetRotation(rotation);
			SetDirty(markDirty);
		}

		void IPart.SetScale(Vector3 scale, DirtyBy markDirty) {
			if (Vector3.Distance(scale, GetScale()) < DefaultThreshold) return;
			SetScale(scale);
			SetDirty(markDirty);
		}

		void IPart.SetVelocity(Vector3 velocity, DirtyBy markDirty) {
			if (Vector3.Distance(velocity, GetVelocity()) < DefaultThreshold) return;
			SetVelocity(velocity);
			SetDirty(markDirty);
			if (markDirty == DirtyBy.Remote && TryGet(out _, out var rigid) && rigid)
				rigid.linearVelocity = velocity;
		}

		void IPart.SetAngularVelocity(Vector3 angularVelocity, DirtyBy markDirty) {
			if (Vector3.Distance(angularVelocity, GetAngularVelocity()) < DefaultThreshold) return;
			SetAngularVelocity(angularVelocity);
			SetDirty(markDirty);
			if (markDirty == DirtyBy.Remote && TryGet(out _, out var rigid) && rigid)
				rigid.angularVelocity = angularVelocity;
		}

		public DirtyBy GetDirty()
			=> _dirty;

		public void SetDirty(DirtyBy by) {
			switch (by) {
				case DirtyBy.Remote:
					_dirty = DirtyBy.None;
					return;
				case DirtyBy.Local:
					_dirty = DirtyBy.Local;
					return;
				case DirtyBy.None:
					_dirty = DirtyBy.None;
					// Ne pas réinitialiser les flags de transformation ici
					// car ils sont nécessaires pour les comparaisons futures
					// ResetPosition();
					// ResetRotation();
					// ResetScale();
					// ResetVelocity();
					// ResetAngularVelocity();
					return;
				default:
					throw new ArgumentOutOfRangeException(nameof(by), by, null);
			}
		}

		public void Update(float time) {
			if (!TryGet(out var transform, out _) || !transform)
				return;

			transform.GetPositionAndRotation(out var position, out var rotation);

			if (!IsSamePosition(position)) {
				var p   = GetPosition();
				var dis = Vector3.Distance(position, p);
				position = dis switch {
					> 10f     => p,
					<= 0.001f => p,
					_         => Vector3.Lerp(position, p, time)
				};
			}

			if (!IsSameRotation(rotation)) {
				var r   = GetRotation();
				var ang = Quaternion.Angle(rotation, r);
				rotation = ang switch {
					> 90f   => r,
					<= 0.1f => r,
					_       => Quaternion.Slerp(rotation, r, time)
				};
			}

			var scale = transform.localScale;
			if (!IsSameScale(scale)) {
				var s   = GetScale();
				var dis = Vector3.Distance(scale, s);
				scale = dis switch {
					> 10f     => s,
					<= 0.001f => s,
					_         => Vector3.Lerp(scale, s, time)
				};
				transform.localScale = scale;
			}

			transform.SetPositionAndRotation(position, rotation);
		}

		public override string ToString()
			=> $"{GetType().Name}[Id={GetId()}, EntityId={Entity.GetId()}, Dirty={GetDirty()}, Flags={Flags}]";
	}
}