using Nox.Entities;

namespace Nox.Players {
	public interface IPlayer : IEntity {
		/// <summary>
		/// Get the display name of the player.
		/// </summary>
		/// <returns></returns>
		public string GetDisplay();

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
	}
}