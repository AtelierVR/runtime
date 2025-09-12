using Nox.Worlds.Spawns;
using UnityEngine;

namespace Nox.CCK.Worlds.Spawns {
	public class TransformSpawn : MonoBehaviour, ISpawn {
		public Vector3 GetPosition()
			=> transform.position;

		public Quaternion GetRotation()
			=> transform.rotation;
	}
}