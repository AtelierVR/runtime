using System.ComponentModel;
using UnityEngine;

namespace Nox.CCK.Worlds.Spawns {
	[DisplayName("Values Spawn")]
	public class TransformSpawn : SpawnBehavior {
		public override Vector3 GetPosition()
			=> transform.position;

		public override Quaternion GetRotation()
			=> transform.rotation;
	}
}