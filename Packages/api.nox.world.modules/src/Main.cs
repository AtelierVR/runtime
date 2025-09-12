using System;
using Nox.Worlds;
using Nox.CCK.Worlds.FellInVoid;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Worlds.Spawns;

namespace api.nox.world.modules {
	public class Main : MainModInitializer {
		internal static MainModCoreAPI      CoreAPI;
		private         EventSubscription[] _events;

		public void OnInitializeMain(MainModCoreAPI api) {
			CoreAPI = api;
			_events = new[] {
				api.EventAPI.Subscribe("world_check_request", OnCheckRequest),
			};
		}

		private static void OnCheckRequest(EventData context) {
			if (!context.TryGet<IWorldDescriptor>(0, out var descriptor))
				return;
			var valid = true;
			valid &= FellInVoidWorldModule.Check(descriptor);
			valid &= SpawnsWorldModule.Check(descriptor);
			context.Callback(valid);
		}

		public void OnDisposeMain() {
			foreach (var e in _events)
				CoreAPI.EventAPI.Unsubscribe(e);
			_events = Array.Empty<EventSubscription>();
			CoreAPI = null;
		}
	}
}