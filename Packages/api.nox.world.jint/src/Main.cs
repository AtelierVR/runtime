using System;
using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.Worlds;

namespace api.nox.session.jint {
	public class Main : MainModInitializer {
		internal static MainModCoreAPI      CoreAPI;
		internal static Main                Instance;
		private         EventSubscription[] _events = Array.Empty<EventSubscription>();

		internal static MainModInitializer WorldAPI
			=> CoreAPI.ModAPI.GetMod("world").GetMains().FirstOrDefault();

		internal static MainModInitializer JintAPI
			=> CoreAPI.ModAPI.GetMod("jint").GetMains().FirstOrDefault();

		public void OnInitializeMain(MainModCoreAPI api) {
			CoreAPI       = api;
			Instance      = this;
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
			Instance = null;
		}
	}
}