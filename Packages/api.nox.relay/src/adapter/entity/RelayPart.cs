using Nox.Entities;
using UnityEngine;

namespace api.nox.relay {
	/// <summary>
	/// Represents a part of a <see cref="RelayEntity"/>.
	/// </summary>
	public class RelayPart : Nox.CCK.Utils.Transform, IPart {
		private readonly RelayEntity _entity;
		private readonly ushort      _id;

		/// <summary>
		/// Initializes a new instance of the <see cref="RelayPart"/> class.
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="id"></param>
		public RelayPart(RelayEntity entity, ushort id) {
			_entity = entity;
			_id     = id;
		}

		/// <summary>
		/// Tries to get the GameObject associated with this part.
		/// </summary>
		/// <param name="gameObject"></param>
		/// <returns></returns>
		private bool TryGet(out GameObject gameObject) {
			if (_entity.TryGetPhysical<RelayPhysicalEntity>(out var physical))
				return physical.TryGetPart(_id, out gameObject);
			gameObject = null;
			return false;
		}

		/// <summary>
		/// Get the ID of the part.
		/// </summary>
		/// <returns></returns>
		public ushort GetId()
			=> _id;

		/// <summary>
		/// Try to get the position of the part.
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public bool TryGetPosition(out Vector3 position) {
			if (!TryGet(out var gameObject)) {
				position = Vector3.zero;
				return false;
			}

			position = gameObject.transform.position;
			return true;
		}

		/// <summary>
		/// Try to get the rotation of the part.
		/// </summary>
		/// <param name="rotation"></param>
		/// <returns></returns>
		public bool TryGetRotation(out Quaternion rotation) {
			if (!TryGet(out var gameObject)) {
				rotation = Quaternion.identity;
				return false;
			}

			rotation = gameObject.transform.rotation;
			return true;
		}

		/// <summary>
		/// Try to get the scale of the part.
		/// </summary>
		/// <param name="scale"></param>
		/// <returns></returns>
		public bool TryGetScale(out Vector3 scale) {
			if (!TryGet(out var gameObject)) {
				scale = Vector3.one;
				return false;
			}

			scale = gameObject.transform.localScale;
			return true;
		}

		/// <summary>
		/// Try to get the velocity of the part.
		/// </summary>
		/// <param name="velocity"></param>
		/// <returns></returns>
		public bool TryGetVelocity(out Vector3 velocity) {
			if (!TryGet(out var gameObject)) {
				velocity = Vector3.zero;
				return false;
			}

			if (!gameObject.TryGetComponent<Rigidbody>(out var rb)) {
				velocity = Vector3.zero;
				return false;
			}

			velocity = rb.linearVelocity;
			return true;
		}

		/// <summary>
		/// Try to get the angular velocity of the part.
		/// </summary>
		/// <param name="angularVelocity"></param>
		/// <returns></returns>
		public bool TryGetAngularVelocity(out Vector3 angularVelocity) {
			if (!TryGet(out var gameObject)) {
				angularVelocity = Vector3.zero;
				return false;
			}

			if (!gameObject.TryGetComponent<Rigidbody>(out var rb)) {
				angularVelocity = Vector3.zero;
				return false;
			}

			angularVelocity = rb.angularVelocity;
			return true;
		}

		/// <summary>
		/// Set the position of the part.
		/// </summary>
		/// <param name="position"></param>
		/// <param name="markDirty"></param>
		public void SetPosition(Vector3 position, bool markDirty) {
			if (TryGet(out var gameObject))
				gameObject.transform.position = position;
			SetPosition(position);
			if (markDirty) SetDirty();
		}

		/// <summary>
		/// Set the rotation of the part.
		/// </summary>
		/// <param name="rotation"></param>
		/// <param name="markDirty"></param>
		public void SetRotation(Quaternion rotation, bool markDirty) {
			if (TryGet(out var gameObject))
				gameObject.transform.rotation = rotation;
			SetRotation(rotation);
			if (markDirty) SetDirty();
		}

		/// <summary>
		/// Set the scale of the part.
		/// </summary>
		/// <param name="scale"></param>
		/// <param name="markDirty"></param>
		public void SetScale(Vector3 scale, bool markDirty) {
			if (TryGet(out var gameObject))
				gameObject.transform.localScale = scale;
			SetScale(scale);
			if (markDirty) SetDirty();
		}

		/// <summary>
		/// Set the velocity of the part.
		/// </summary>
		/// <param name="velocity"></param>
		/// <param name="markDirty"></param>
		public void SetVelocity(Vector3 velocity, bool markDirty) {
			if (TryGet(out var gameObject) && gameObject.TryGetComponent<Rigidbody>(out var rb))
				rb.linearVelocity = velocity;
			SetVelocity(velocity);
			if (markDirty) SetDirty();
		}

		/// <summary>
		/// Set the angular velocity of the part.
		/// </summary>
		/// <param name="angularVelocity"></param>
		/// <param name="markDirty"></param>
		public void SetAngularVelocity(Vector3 angularVelocity, bool markDirty) {
			if (TryGet(out var gameObject) && gameObject.TryGetComponent<Rigidbody>(out var rb))
				rb.angularVelocity = angularVelocity;
			SetAngularVelocity(angularVelocity);
			if (markDirty) SetDirty();
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
			if (!TryGet(out var gameObject)) return;
			gameObject.transform.GetPositionAndRotation(out var position, out var rotation);

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

			gameObject.transform.SetPositionAndRotation(position, rotation);
		}
	}
}