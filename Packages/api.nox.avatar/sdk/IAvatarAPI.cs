using Cysharp.Threading.Tasks;

namespace Nox.Avatars {
	public interface IAvatarAPI {
		public UniTask<IAvatar> MakeLoading();
		public UniTask<IAvatar> MakeDefault();
		public UniTask<IAvatar> MakeError();
	}
}