using System;
using Cysharp.Threading.Tasks;

namespace Nox.Avatars {
	public interface IRuntimeAvatar {
		/// <summary>
		/// Gets the unique identifier of the avatar.
		/// </summary>
		/// <returns></returns>
		public string GetId();

		/// <summary>
		/// Gets the avatar descriptor, which contains metadata about the avatar.
		/// </summary>
		/// <returns></returns>
		public IAvatarDescriptor GetDescriptor();
		
		

		/// <summary>
		/// Disposes of the avatar and releases any resources it holds.
		/// </summary>
		/// <returns></returns>
		public UniTask Dispose();
	}
}