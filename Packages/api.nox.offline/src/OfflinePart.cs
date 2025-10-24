using System.Collections.Generic;
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

		public bool IsDirty()
			=> false;

		public void SetDirty(bool dirty = true) { }

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

		public void SetPosition(Vector3 position, bool markDirty = true)
			=> _transform.position = position;

		public void SetRotation(Quaternion rotation, bool markDirty = true)
			=> _transform.rotation = rotation;

		public void SetScale(Vector3 scale, bool markDirty = true)
			=> _transform.localScale = scale;

		public void SetVelocity(Vector3 velocity, bool markDirty = true) {
			if (!_transform.TryGetComponent(out Rigidbody rb)) return;
			rb.linearVelocity = velocity;
		}

		public void SetAngularVelocity(Vector3 angularVelocity, bool markDirty = true) {
			if (!_transform.TryGetComponent(out Rigidbody rb)) return;
			rb.angularVelocity = angularVelocity;
		}
	}
}