using System;
using System.IO;
using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Jint;
using Nox.Worlds;

namespace api.nox.session.jint {
	public class Main : IMainModInitializer {
		internal static MainModCoreAPI      CoreAPI;
		private         EventSubscription[] _events = Array.Empty<EventSubscription>();

		internal static IWorldAPI WorldAPI
			=> CoreAPI.ModAPI
				.GetMod("world")
				.GetInstance<IWorldAPI>();

		internal static IJintAPI JintAPI
			=> CoreAPI.ModAPI
				.GetMod("jint")
				.GetInstance<IJintAPI>();

		public void OnInitializeMain(MainModCoreAPI api) {
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