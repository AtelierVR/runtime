using Cysharp.Threading.Tasks;

namespace Nox.Avatars {
	public interface IAvatarAPI {
		/// <summary>
		/// Creates a loading avatar.
		/// </summary>
		/// <returns></returns>
		public UniTask<IRuntimeAvatar> MakeLoading();

		/// <summary>
		/// Creates a default avatar.
		/// </summary>
		/// <returns></returns>
		public UniTask<IRuntimeAvatar> MakeDefault();

		/// <summary>
		/// Creates an error avatar.
		/// </summary>
		/// <returns></returns>
		public UniTask<IRuntimeAvatar> MakeError();
	}
}