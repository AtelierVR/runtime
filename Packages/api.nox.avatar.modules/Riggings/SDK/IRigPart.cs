using UnityEngine;

namespace Nox.Avatars.Rigging {
	public interface IRigPart {
		public ushort GetId();

		public Transform GetTransform();

		public Rigidbody GetRigidbody();
	}
}