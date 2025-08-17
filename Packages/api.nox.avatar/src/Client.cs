using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;

namespace api.nox.avatar {
	public class Client : ClientModInitializer, IAvatarAPI {
		public void OnInitializeClient(ClientModCoreAPI api) { }

		public void OnDisposeClient() { }

		public async UniTask<IAvatar> MakeLoading() {
			await UniTask.Yield();
			Logger.LogWarning("Avatar API is not implemented in the client mod. This is a stub implementation.");
			return null;
		}

		public async UniTask<IAvatar> MakeDefault() {
			await UniTask.Yield();
			Logger.LogWarning("Avatar API is not implemented in the client mod. This is a stub implementation.");
			return null;
		}

		public async UniTask<IAvatar> MakeError() {
			await UniTask.Yield();
			Logger.LogWarning("Avatar API is not implemented in the client mod. This is a stub implementation.");
			return null;
		}
	}
}