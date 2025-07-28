using System;
using System.Linq;
using api.nox.server.network;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Network;
using Nox.Servers;
using Nox.Users;
using UnityEngine.Events;

namespace api.nox.server {
	public class Main : MainModInitializer, IServerAPI {
		internal        MainModCoreAPI CoreAPI;
		internal static Main           Instance;

		internal Network                         Network;
		internal (IUserIdentifier, ServerSocket) Socket = (null, null);

		internal static INetworkAPI NetworkAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("network")
				.GetMains()
				.FirstOrDefault() as INetworkAPI;

		internal static IUserAPI UserAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("user")
				.GetMains()
				.FirstOrDefault() as IUserAPI;

		internal readonly UnityEvent<INoxObject> OnServerUpdated      = new();
		internal readonly UnityEvent<INoxObject> OnServerFetched      = new();
		internal readonly UnityEvent<INoxObject> OnServerConnected    = new();
		internal readonly UnityEvent<INoxObject> OnServerDisconnected = new();

		private EventSubscription[] _events = Array.Empty<EventSubscription>();

		public void OnInitializeMain(MainModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;
			Network  = new Network();
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
				),
				CoreAPI.EventAPI.Subscribe("user_update", OnCurrentUserUpdated)
			};
		}

		private void OnCurrentUserUpdated(EventData context) {
			var user = context.TryGet(0, out IUser u) ? u : null;
			StartCurrentSocket(user).Forget();
		}


		public async UniTask OnPostInitializeMainAsync() {
			var user = UserAPI.GetCurrent() ?? await UserAPI.FetchCurrent();
			await StartCurrentSocket(user);
		}

		private async UniTask StartCurrentSocket(IUser user) {
			if (Socket.Item2 != null && Socket.Item1.Equals(user.ToIdentifier())) {
				Logger.LogDebug("Already connected to server for current user.");
				return;
			}

			if (Socket.Item2 != null)
				await Socket.Item2.Dispose();

			Socket = (user.ToIdentifier(), null);

			var address = user?.GetServerAddress();
			if (address == null) {
				Logger.LogWarning("Current user has no server address set, cannot connect to server.");
				return;
			}

			var token = await UserAPI.GetToken(address);

			var socket = await ServerSocket.Make(address, token);
			if (socket == null) {
				Logger.LogError($"Failed to connect to server at {address}");
				return;
			}

			Socket = (user.ToIdentifier(), socket);

			socket.OnMessageReceived.AddListener(Logger.LogDebug);
			socket.OnError.AddListener(Logger.LogException);
			socket.OnConnected.AddListener(() => Logger.LogDebug("Connected to server"));
			socket.OnDisconnected.AddListener(() => Logger.LogDebug("Disconnected from server"));

			await Socket.Item2.Connect();
		}

		public void OnDisposeMain() {
			foreach (var ev in _events.Where(e => e != null))
				CoreAPI.EventAPI.Unsubscribe(ev);
			Network  = null;
			CoreAPI  = null;
			Instance = null;
		}

		public async UniTask<IServer> Fetch(string from = null)
			=> await Network.Fetch(from);

		public async UniTask<IServerSocket> Connect(string address)
			=> await ServerSocket.Make(address);
	}
}