using Nox.Worlds.Spawns;
using UnityEngine;

namespace Nox.CCK.Worlds.Spawns {
	public class StructSpawn : ISpawn {
		public StructSpawn(Transform transform)
			: this(transform.position, transform.rotation) { }

		public StructSpawn(Vector3 position, Quaternion rotation) {
			_position = position;
			_rotation = rotation;
		}

		private readonly Vector3    _position;
		private readonly Quaternion _rotation;


		public Vector3 GetPosition()
			=> _position;

		public Quaternion GetRotation()
			=> _rotation;
	}
}