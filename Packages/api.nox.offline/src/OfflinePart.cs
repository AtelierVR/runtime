using System.Collections.Generic;
using Nox.CCK.Network;
using Nox.Entities;
using UnityEngine;

namespace api.nox.offline {
	public class OfflinePart : IPart {
		private readonly ushort    _id;
		private readonly Transform _transform;

		public OfflinePart(KeyValuePair<ushort, Transform> pair) {
			_id        = pair.Key;
			_transform = pair.Value;
		}

		public DirtyBy GetDirty()
			=> DirtyBy.None;

		public void SetDirty(DirtyBy dirty) { }

		public ushort GetId()
			=> _id;

		public bool TryGetPosition(out Vector3 position) {
			position = _transform.position;
			return true;
		}

		public bool TryGetRotation(out Quaternion rotation) {
			rotation = _transform.rotation;
			return true;
		}

		public bool TryGetScale(out Vector3 scale) {
			scale = _transform.localScale;
			return true;
		}

		public bool TryGetAngularVelocity(out Vector3 angularVelocity) {
			if (_transform.TryGetComponent(out Rigidbody rb)) {
				angularVelocity = rb.angularVelocity;
				return true;
			}

			angularVelocity = Vector3.zero;
			return false;
		}

		public bool TryGetVelocity(out Vector3 transform) {
			if (_transform.TryGetComponent(out Rigidbody rb)) {
				transform = rb.linearVelocity;
				return true;
			}

			transform = Vector3.zero;
			return false;
		}

		public void SetPosition(Vector3 position, DirtyBy markDirty)
			=> _transform.position = position;

		public void SetRotation(Quaternion rotation, DirtyBy markDirty)
			=> _transform.rotation = rotation;

		public void SetScale(Vector3 scale, DirtyBy markDirty)
			=> _transform.localScale = scale;

		public void SetVelocity(Vector3 velocity, DirtyBy markDirty) {
			if (!_transform.TryGetComponent(out Rigidbody rb)) return;
			rb.linearVelocity = velocity;
		}

		public void SetAngularVelocity(Vector3 angularVelocity, DirtyBy markDirty) {
			if (!_transform.TryGetComponent(out Rigidbody rb)) return;
			rb.angularVelocity = angularVelocity;
		}

		public override string ToString()
			=> $"{GetType().Name}[Id={GetId()}, Transform={_transform}]";
	}
}