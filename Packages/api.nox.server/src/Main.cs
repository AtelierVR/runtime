using System;
using System.Linq;
using api.nox.server.network;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Network;
using Nox.Servers;
using Nox.Users;
using UnityEngine.Events;

namespace api.nox.server
{
	public class Main : IMainModInitializer, IServerAPI
	{
		internal IMainModCoreAPI CoreAPI;
		internal static Main Instance;

		private LanguagePack _lang;
		internal (Identifier, ServerSocket) Socket = (Identifier.Invalid, null);
		private readonly object _socketLock = new object();
		private bool _isConnecting;

		internal static INetworkAPI NetworkAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("network")
				?.GetInstance<INetworkAPI>();

		internal static IUserAPI UserAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("users")
				?.GetInstance<IUserAPI>();

		internal readonly UnityEvent<INoxObject> OnServerUpdated = new();
		internal readonly UnityEvent<INoxObject> OnServerFetched = new();
		internal readonly UnityEvent<INoxObject> OnServerConnected = new();
		internal readonly UnityEvent<INoxObject> OnServerDisconnected = new();
		internal readonly UnityEvent OnSocketConnected = new();
		internal readonly UnityEvent OnSocketDisconnected = new();

		UnityEvent IServerAPI.OnSocketConnected    => OnSocketConnected;
		UnityEvent IServerAPI.OnSocketDisconnected => OnSocketDisconnected;

		private EventSubscription[] _events = Array.Empty<EventSubscription>();

		public void OnInitializeMain(IMainModCoreAPI api)
		{
			CoreAPI = api;
			Instance = this;
			_lang = CoreAPI.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_lang);

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

		private void OnCurrentUserUpdated(EventData context)
		{
			var user = context.TryGet(0, out IUser u) ? u : null;
			StartCurrentSocket(user).Forget();
		}


		public async UniTask OnPostInitializeMainAsync()
		{
			var user = UserAPI.Current ?? await UserAPI.FetchCurrent();
			await StartCurrentSocket(user);
		}

		private async UniTask StartCurrentSocket(IUser user)
		{
			// Prévenir les connexions concurrentes
			lock (_socketLock)
			{
				if (_isConnecting)
				{
					Logger.LogDebug("Socket connection already in progress, skipping.");
					return;
				}
				_isConnecting = true;
			}

			try
			{
				if (Socket.Item2 != null && Socket.Item1.IsValid() && Socket.Item1.Equals(user?.Identifier))
				{
					Logger.LogDebug("Already connected to server for current user.");
					return;
				}

				if (Socket.Item2 != null)
					await Socket.Item2.Dispose();

				if (user == null)
				{
					Logger.LogDebug("No current user, not connecting to server.");
					Socket = (Identifier.Invalid, null);
					return;
				}

				Socket = (user.Identifier, null);

				var address = user.Server;
				if (address == null)
				{
					Logger.LogWarning("Current user has no server address set, cannot connect to server.");
					return;
				}

				var token = await UserAPI.GetToken(address);

				var socket = await ServerSocket.Make(address, token);
				if (socket == null)
				{
					Logger.LogError($"Failed to connect to server at {address}");
					return;
				}

				Socket = (user.Identifier, socket);

				socket.OnMessageReceived.AddListener(Instance.CoreAPI.LoggerAPI.LogDebug);
				socket.OnError.AddListener(Instance.CoreAPI.LoggerAPI.LogException);
				socket.OnConnected.AddListener(() => {
					Logger.LogDebug("Connected to server");
					CoreAPI.EventAPI.Emit("socket_connect");
					OnSocketConnected.Invoke();
				});
				socket.OnDisconnected.AddListener(() => {
					Logger.LogDebug("Disconnected from server");
					CoreAPI.EventAPI.Emit("socket_disconnect");
					OnSocketDisconnected.Invoke();
				});

				await Socket.Item2.Connect();
			}
			finally
			{
				lock (_socketLock)
				{
					_isConnecting = false;
				}
			}
		}

		public async UniTask OnDisposeMainAsync()
		{
			foreach (var ev in _events.Where(e => e != null))
				CoreAPI.EventAPI.Unsubscribe(ev);
			LanguageManager.RemovePack(_lang);

			if (Socket.Item2 != null)
				await Socket.Item2.Dispose();

			Socket = (Identifier.Invalid, null);

			CoreAPI = null;
			Instance = null;
		}

		public async UniTask<IServer> Fetch(string from = null)
			=> await Network.Fetch(from);

		public async UniTask<IServerSocket> Connect(string address)
			=> await ServerSocket.Make(address);

		public IServerSocket Current 
			=> Socket.Item2;

		// ── Packet helpers (delegate to Current socket) ────────────────────────────────────

		public UniTask Emit<T>(SocketPacket<T> packet)
			=> Current?.Emit(packet) ?? UniTask.CompletedTask;

		public void On<T>(string type, Action<SocketPacket<T>> handler)
			=> Current?.On(type, handler);

		public void Off<T>(string type, Action<SocketPacket<T>> handler)
			=> Current?.Off(type, handler);

		public void Once<T>(string type, Action<SocketPacket<T>> handler)
			=> Current?.Once(type, handler);
	}
}