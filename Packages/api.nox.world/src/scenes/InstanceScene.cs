using Nox.Worlds;
using UnityEngine;

namespace api.nox.world {
	public class InstanceScene {
		public GameObject       Container;
		public IWorldDescriptor Descriptor;
		public bool             Visible = false;

		public int GetId()
			=> Descriptor.GetAnchor().GetInstanceID();
	}
}