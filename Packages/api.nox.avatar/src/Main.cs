using api.nox.avatar.network;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Network;
using Nox.Tables;
using Nox.Users;

namespace api.nox.avatar {
	public class Main : MainModInitializer, IAvatarAPI {
		public static Main           Instance;
		public        MainModCoreAPI CoreAPI;
		internal      Network        Network;

		internal INetworkAPI NetworkAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("network")
				?.GetEntry<INetworkAPI>();

		internal IUserAPI UserAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("user")
				?.GetEntry<IUserAPI>();

		internal ITableAPI TableAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("table")
				?.GetEntry<ITableAPI>();

		public void OnInitializeMain(MainModCoreAPI api) {
			Instance = this;
			CoreAPI  = api;
			Network  = new Network();
		}

		public void OnDisposeMain() {
			Network  = null;
			CoreAPI  = null;
			Instance = null;
		}

		public async UniTask<IRuntimeAvatar> MakeLoading() {
			var     config = Config.Load();
			var     custom = config.Get<string>(new[] { "avatar", "loading" });
			IRuntimeAvatar runtimeAvatar = null;
			if (!string.IsNullOrEmpty(custom))
				runtimeAvatar = await AvatarLoader.LoadFromCache(custom);
			runtimeAvatar ??= await AvatarLoader.LoadFromAssets(CoreAPI.ModMetadata.GetId(), "prefabs/loading.prefab");
			runtimeAvatar ??= await MakeError();
			return runtimeAvatar;
		}

		public async UniTask<IRuntimeAvatar> MakeDefault() {
			var     config = Config.Load();
			var     custom = config.Get<string>(new[] { "avatar", "default" });
			IRuntimeAvatar runtimeAvatar = null;
			if (!string.IsNullOrEmpty(custom))
				runtimeAvatar = await AvatarLoader.LoadFromCache(custom);
			runtimeAvatar ??= await AvatarLoader.LoadFromAssets(CoreAPI.ModMetadata.GetId(), "prefabs/default.prefab");
			runtimeAvatar ??= await MakeError();
			return runtimeAvatar;
		}

		public async UniTask<IRuntimeAvatar> MakeError() {
			var     config = Config.Load();
			var     custom = config.Get<string>(new[] { "avatar", "error" });
			IRuntimeAvatar runtimeAvatar = null;
			if (!string.IsNullOrEmpty(custom))
				runtimeAvatar = await AvatarLoader.LoadFromCache(custom);
			runtimeAvatar ??= await AvatarLoader.LoadFromAssets(CoreAPI.ModMetadata.GetId(), "prefabs/error.prefab");
			return runtimeAvatar;
		}
	}
}