using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;

namespace api.nox.avatar {
	public class Main : MainModInitializer, IAvatarAPI {
		public static Main           Instance;
		public        MainModCoreAPI CoreAPI;

		public void OnInitializeMain(MainModCoreAPI api) {
			Instance = this;
			CoreAPI  = api;
		}

		public void OnDisposeMain() {
			CoreAPI  = null;
			Instance = null;
		}

		public async UniTask<IAvatar> MakeLoading() {
			var     config = Config.Load();
			var     custom = config.Get<string>(new[] { "avatar", "loading" });
			IAvatar avatar = null;
			if (!string.IsNullOrEmpty(custom))
				avatar = await AvatarLoader.LoadFromCache(custom);
			avatar ??= await AvatarLoader.LoadFromAssets(CoreAPI.ModMetadata.GetId(), "prefabs/loading.prefab");
			avatar ??= await MakeError();
			return avatar;
		}

		public async UniTask<IAvatar> MakeDefault() {
			var     config = Config.Load();
			var     custom = config.Get<string>(new[] { "avatar", "default" });
			IAvatar avatar = null;
			if (!string.IsNullOrEmpty(custom))
				avatar = await AvatarLoader.LoadFromCache(custom);
			avatar ??= await AvatarLoader.LoadFromAssets(CoreAPI.ModMetadata.GetId(), "prefabs/default.prefab");
			avatar ??= await MakeError();
			return avatar;
		}

		public async UniTask<IAvatar> MakeError() {
			var     config = Config.Load();
			var     custom = config.Get<string>(new[] { "avatar", "error" });
			IAvatar avatar = null;
			if (!string.IsNullOrEmpty(custom))
				avatar = await AvatarLoader.LoadFromCache(custom);
			avatar ??= await AvatarLoader.LoadFromAssets(CoreAPI.ModMetadata.GetId(), "prefabs/error.prefab");
			return avatar;
		}
	}
}