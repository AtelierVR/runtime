using Nox.CCK.Worlds;
using UnityEngine;

namespace api.nox.world {
	public class InstanceScene<T> where T : BaseDescriptor {
		public GameObject Container;
		public T          Descriptor;
		public bool       Visible = false;
		
		public int GetId()
			=> Descriptor.GetInstanceID();
	}
}