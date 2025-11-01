using Nox.Entities;
using UnityEngine;

namespace api.nox.relay {
	public abstract class RelayPhysicalEntity : Physical {
		public Rigidbody body;

		public new Rigidbody rigidbody
			=> body ??= GetComponent<Rigidbody>();

		public abstract bool TryGetPart(ushort id, out Transform go, out Rigidbody rigid);
	}
}