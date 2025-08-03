using Nox.Players;

namespace Nox.Avatars {
	public interface IAvatarDescriptor {
		/// <summary>
		/// Sets the reference to the player that this avatar is associated with.
		/// </summary>
		/// <param name="player"></param>
		public void SetPlayer(IPlayer player);

		/// <summary>
		/// Gets the player associated with this avatar descriptor.
		/// </summary>
		/// <returns></returns>
		public IPlayer GetPlayer();

		/// <summary>
		/// Gets the avatar modules of a specific type.
		/// Is used to retrieve modules that implement a specific interface.
		/// Like Voice, Eyes, etc.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public IAvatarModule[] GetModules<T>() where T : IAvatarModule;
		public IAvatarModule[] GetModules();
	}
}