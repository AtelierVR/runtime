using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Cysharp.Threading.Tasks;
using Nox.Worlds;
using Nox.Offline;
using Nox.Sessions;

namespace api.nox.main {
	public class Client : ClientModInitializer {
		private static ClientModCoreAPI _coreAPI;

		public void OnInitializeClient(ClientModCoreAPI api)
			=> _coreAPI = api;


		private static T GetMainClass<T>(string id) where T : class
			=> _coreAPI.ModAPI
				.GetMod(id)
				.GetMains()
				.FirstOrDefault() as T;

		private static IWorldAPI WorldAPI
			=> GetMainClass<IWorldAPI>("world");

		private static IOfflineAPI OfflineAPI
			=> GetMainClass<IOfflineAPI>("offline");

		private static ISessionAPI SessionAPI
			=> GetMainClass<ISessionAPI>("session");

		public async UniTask OnPostInitializeClientAsync() {
			var world = await WorldAPI.LoadSceneFromAssets(
				_coreAPI.ModMetadata.GetId(),
				"worlds/default/default.unity"
			);
			var adapter = OfflineAPI.New();
			adapter.SetDimension(world);
			var session = SessionAPI.New(adapter);
			await session.SetCurrent();
		}

		public void OnDisposeClient() { }
	}
}