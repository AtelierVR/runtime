using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Worlds;
using Nox.Offline;
using Nox.Sessions;
using UnityEngine.Scripting;

namespace api.nox.main {
	[Preserve]
	public class Client : IClientModInitializer {
		private static ClientModCoreAPI _coreAPI;

		public void OnInitializeClient(ClientModCoreAPI api) 
			=> _coreAPI = api;

		private static IWorldAPI WorldAPI
			=> _coreAPI.ModAPI
				.GetMod("world")
				?.GetInstance<IWorldAPI>();

		private static IOfflineAPI OfflineAPI
			=> _coreAPI.ModAPI
				.GetMod("offline")
				?.GetInstance<IOfflineAPI>();

		private static ISessionAPI SessionAPI
			=> _coreAPI.ModAPI
				.GetMod("session")
				?.GetInstance<ISessionAPI>();

		public async UniTask OnPostInitializeClientAsync() {
			var world = await WorldAPI.LoadFromAssets(
				_coreAPI.ModMetadata.GetId(),
				"worlds/default/default.unity"
			);
			var adapter = OfflineAPI.New();
			adapter.SetDimension(world);
			var session = SessionAPI.New(adapter);
			await session.SetCurrent();
		}
	}
}