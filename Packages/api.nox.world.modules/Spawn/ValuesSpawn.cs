using Nox.Worlds.Spawns;
using UnityEngine;

namespace Nox.CCK.Worlds.Spawns {
	public class ValuesSpawn : MonoBehaviour, ISpawn {
		public Vector3    position = Vector3.zero;
		public Quaternion rotation = Quaternion.identity;

		public Vector3 GetPosition()
			=> position;

		public Quaternion GetRotation()
			=> rotation;
	}
}