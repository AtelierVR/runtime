using Nox.Avatars.Rigging;
using UnityEngine;

namespace Nox.CCK.Avatars.Rigging {
	public class RiggingPart : IRigPart {
		private readonly ushort    _bone;
		private          Transform _anchor;
		private          Rigidbody _rigidbody;

		public RiggingPart(ushort bone, Transform anchor) {
			_bone = bone;
			SetTransform(anchor);
		}

		public ushort GetId()
			=> _bone;

		internal void SetTransform(Transform part) {
			_anchor    = part;
			_rigidbody = part.gameObject.GetComponent<Rigidbody>();
		}

		public Transform GetTransform()
			=> _anchor;

		// ReSharper disable Unity.PerformanceAnalysis
		public Rigidbody GetRigidbody()
			=> _rigidbody ??= _anchor.gameObject.GetComponent<Rigidbody>();
	}
}