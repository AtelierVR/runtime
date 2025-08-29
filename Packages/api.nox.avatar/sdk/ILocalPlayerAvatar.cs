using Cysharp.Threading.Tasks;

namespace Nox.Avatars.Players {
	public interface ILocalPlayerAvatar : IPlayerAvatar {
		public UniTask<bool> SendAvatarReady();
		public UniTask<bool> SendAvatarFailed(string reason);
	}
}