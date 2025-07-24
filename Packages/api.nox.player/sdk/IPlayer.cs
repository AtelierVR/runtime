using Nox.Entities;
using Nox.Users;
using UnityEngine;

namespace Nox.Players {
	public interface IPlayer : IEntity {
		/// <summary>
		/// Get the display name of the player.
		/// </summary>
		/// <returns></returns>
		public string GetDisplay();

		/// <summary>
		/// Get the player Identifier (ID).
		/// </summary>
		/// <returns></returns>
		public IUserIdentifier ToIdentifier();

		/// <summary>
		/// Check if the player is the master player.
		/// </summary>
		/// <returns></returns>
		public bool IsMaster();

		/// <summary>
		/// Check if the player is a local player.
		/// </summary>
		/// <returns></returns>
		public bool IsLocal();

		/// <summary>
		/// Set the display name of the player.
		/// </summary>
		/// <param name="display"></param>
		public void SetDisplay(string display);

		/// <summary>
		/// Teleport the player to a specific position and rotation.
		/// </summary>
		/// <param name="position">Target position</param>
		/// <param name="rotation">Target rotation</param>
		public void Teleport(Vector3 position, Quaternion rotation);

		/// <summary>
		/// Teleport the player to a transform's position and rotation.
		/// </summary>
		/// <param name="transform">Target transform</param>
		public void Teleport(Transform transform);
	}
}