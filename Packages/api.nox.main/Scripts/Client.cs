using System.Collections.Generic;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Cysharp.Threading.Tasks;
using Nox.CCK.Sessions;
using Nox.CCK.Utils;
using Nox.Worlds;
using Nox.Sessions;
using UnityEngine;

namespace api.nox.main {
	public class Client : IClientModInitializer {
		private static IClientModCoreAPI _coreAPI;

		public void OnInitializeClient(IClientModCoreAPI api) {
			Physics.simulationMode = SimulationMode.Script;
			_coreAPI               = api;
		}

		private static IWorldAPI WorldAPI
			=> _coreAPI.ModAPI
				.GetMod("world")
				?.GetInstance<IWorldAPI>();

		private static ISessionAPI SessionAPI
			=> _coreAPI.ModAPI
				.GetMod("session")
				?.GetInstance<ISessionAPI>();

		public async UniTask OnPostInitializeClientAsync() {
			if (!SessionAPI.TryMake(
				    "offline",
				    new Dictionary<string, object> {
					    ["world"]       = new ResourceIdentifier(_coreAPI.ModMetadata.GetId(), "worlds/default/default.unity"),
					    ["set_current"] = true
				    },
				    out var session
			    )) {
				_coreAPI.LoggerAPI.LogError("Failed to create offline session in client mod initializer.");
				return;
			}

			var ready = await session.WhenFinished();
			if (!ready) {
				_coreAPI.LoggerAPI.LogError("Offline session failed to become ready in client mod initializer.");
				return;
			}

			_coreAPI.LoggerAPI.Log("Offline session is ready in client mod initializer.");
			Physics.simulationMode = SimulationMode.Update;
		}
	}
}