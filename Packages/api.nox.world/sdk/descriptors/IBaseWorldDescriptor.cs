using UnityEngine;

namespace Nox.Worlds {
	public interface IBaseWorldDescriptor {
		/// <summary>
		/// Returns the root GameObject of the descriptor.
		/// </summary>
		/// <returns></returns>
		public GameObject GetRoot();

		/// <summary>
		/// Returns the unique identifier of the scene in the world.
		/// </summary>
		/// <returns></returns>
		public GameObject[] GetSpawns();

		/// <summary>
		/// Returns the spawn selection type for the world.
		/// </summary>
		/// <returns></returns>
		public SpawnType GetSpawnType();

		/// <summary>
		/// Returns the unique identifier of the world.
		/// </summary>
		/// <returns></returns>
		public int GetSpawnIndex();

		/// <summary>
		/// Picks the next spawn index for the world.
		/// </summary>
		/// <returns></returns>
		public int NextSpawnIndex();
	}
}