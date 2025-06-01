using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using UnityEngine.Events;

namespace api.nox.server {
	public class ServerSystem : MainModInitializer {
		internal static MainModCoreAPI CoreAPI;
		internal static ServerSystem   Instance;

		internal static MainModInitializer NetworkAPI
			=> CoreAPI.ModAPI
				.GetMod("network")
				.GetMains()
				.FirstOrDefault();

		internal static INoxObject ServerAPI
			=> NetworkAPI.GetField("Server");

		internal readonly UnityEvent<INoxObject> OnServerUpdated      = new();
		internal readonly UnityEvent<INoxObject> OnServerFetched      = new();
		internal readonly UnityEvent<INoxObject> OnServerConnected    = new();
		internal readonly UnityEvent<INoxObject> OnServerDisconnected = new();

		private EventSubscription[] _events = Array.Empty<EventSubscription>();

		public void OnInitializeMain(MainModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;
			_events = new[] {
				CoreAPI.EventAPI.Subscribe(
					"server_update",
					data => OnServerUpdated.Invoke(
						data.TryGet(0, out INoxObject server)
							? server
							: null
					)
				),
				CoreAPI.EventAPI.Subscribe(
					"server_fetch",
					data => OnServerFetched.Invoke(
						data.TryGet(0, out INoxObject server)
							? server
							: null
					)
				),
				CoreAPI.EventAPI.Subscribe(
					"server_connect",
					data => OnServerConnected.Invoke(
						data.TryGet(0, out INoxObject server)
							? server
							: null
					)
				),
				CoreAPI.EventAPI.Subscribe(
					"server_disconnect",
					data => OnServerDisconnected.Invoke(
						data.TryGet(0, out INoxObject server)
							? server
							: null
					)
				)
			};
		}

		public async UniTask OnPostInitializeMainAsync() {
			var server = ServerAPI.CallMethod("GetCurrentServer");
			server ??= await ServerAPI.CallAsyncMethod("GetMyServer");

			if (server == null) {
				Logger.Log("Server not detected");
				return;
			}

			var title = server.GetField<string>("title");
			Logger.Log($"Server {title} detected");
		}

		public void OnDisposeMain() {
			foreach (var ev in _events.Where(e => e != null))
				CoreAPI.EventAPI.Unsubscribe(ev);
			CoreAPI  = null;
			Instance = null;
		}
	}
}