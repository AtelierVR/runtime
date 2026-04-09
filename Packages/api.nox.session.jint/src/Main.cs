using System;
using System.IO;
using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Jint;
using Nox.Tables;
using Nox.Worlds;

namespace api.nox.session.jint {
	public class Main : IMainModInitializer {
		static internal IMainModCoreAPI      CoreAPI;
		private         EventSubscription[] _events = Array.Empty<EventSubscription>();

		static internal IWorldAPI WorldAPI
			=> CoreAPI.ModAPI
				.GetMod("world")
				.GetInstance<IWorldAPI>();

		static internal ITableAPI TableAPI
			=> CoreAPI.ModAPI
				.GetMod("tables")
				.GetInstance<ITableAPI>();

		static internal IJintAPI JintAPI
			=> CoreAPI.ModAPI
				.GetMod("jint")
				.GetInstance<IJintAPI>();

		public void OnInitializeMain(IMainModCoreAPI api) {
			CoreAPI  = api;
			_events = new[] {
				api.EventAPI.Subscribe("world_check_request", OnCheckRequest),
			};
		}

		private static void OnCheckRequest(EventData context) {
			if (!context.TryGet<IWorldDescriptor>(0, out var descriptor))
				return;
			var valid = true;
			valid &= JintBackingModule.Check(descriptor);
			context.Callback(valid);
		}

		public void OnDisposeMain() {
			foreach (var e in _events)
				CoreAPI.EventAPI.Unsubscribe(e);
			_events  = Array.Empty<EventSubscription>();
			CoreAPI  = null;
		}
	}
}