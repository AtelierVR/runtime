using UnityEngine;

namespace Nox.Worlds.Spawns {
	public interface ISpawn {
		public Vector3    GetPosition();
		public Quaternion GetRotation();
	}
}