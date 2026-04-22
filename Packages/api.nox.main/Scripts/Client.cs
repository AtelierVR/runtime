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
				.GetMod("worlds")
				?.GetInstance<IWorldAPI>();

		private static ISessionAPI SessionAPI
			=> _coreAPI.ModAPI
				.GetMod("session")
				?.GetInstance<ISessionAPI>();

		public async UniTask OnPostInitializeClientAsync() {
			// Lecture de la config pour déterminer le world à charger
			var config = Config.Load();
			var sessionOptions = new Dictionary<string, object> {
				["set_current"] = true
			};

			var worldHash = config.Has("world.hash") ? config.Get<string>("world.hash") : null;
			if (!string.IsNullOrEmpty(worldHash) && WorldAPI?.HasInCache(worldHash) == true) {
				// Hash présent dans la config et fichier trouvé dans le cache local : chargement sans fetch réseau
				sessionOptions["world_hash"] = worldHash;
				var worldIdStr = config.Get<string>("world.id");
				if (!string.IsNullOrEmpty(worldIdStr))
					sessionOptions["world_identifier"] = worldIdStr;
				_coreAPI.LoggerAPI.Log($"Loading world from local cache using hash '{worldHash}'.");
			} else {
				// Fallback : world par défaut embarqué dans les assets
				if (!string.IsNullOrEmpty(worldHash))
					_coreAPI.LoggerAPI.LogWarning($"World hash '{worldHash}' specified in config but not found in cache. Falling back to default world.");
				sessionOptions["world"] = new ResourceIdentifier(_coreAPI.ModMetadata.GetId(), "worlds/default/default.unity");
			}

			if (!SessionAPI.TryMake(
				    "offline",
				    sessionOptions,
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