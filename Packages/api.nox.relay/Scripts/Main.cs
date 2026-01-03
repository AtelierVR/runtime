using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using api.nox.relay.connection;
using api.nox.relay.connector;
using Nox.Avatars;
using Nox.CCK.Mods.Events;
using Nox.Controllers;
using Nox.Entities;
using Nox.Instances;
using Nox.Microphone;
using Nox.Network;
using Nox.Sessions;
using Nox.Users;
using Nox.Worlds;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace api.nox.relay {
	public class Main : IMainModInitializer {
		public readonly List<Connection>    Connections = new();
		public static   Main                Instance;
		internal        IMainModCoreAPI      CoreAPI;
		private         EventSubscription[] _events = Array.Empty<EventSubscription>();

		internal static IEntityAPI EntityAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("entity")
				.GetInstance<IEntityAPI>();

		internal static INetworkAPI NetworkAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("network")
				.GetInstance<INetworkAPI>();

		internal static IUserAPI UserAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("user")
				.GetInstance<IUserAPI>();

		internal static IWorldAPI WorldAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("world")
				.GetInstance<IWorldAPI>();

		internal static IAvatarAPI AvatarAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("avatar")
				.GetInstance<IAvatarAPI>();

		internal static ISessionAPI SessionAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("session")
				.GetInstance<ISessionAPI>();

		internal static IInstanceAPI InstanceAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("instance")
				.GetInstance<IInstanceAPI>();

		internal static IControllerAPI ControllerAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("controller")
				.GetInstance<IControllerAPI>();

		internal static IMicrophoneAPI MicrophoneAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("microphone")
				.GetInstance<IMicrophoneAPI>();

		public static UnityEvent<Connection> OnConnectionAdded   = new();
		public static UnityEvent<Connection> OnConnectionRemoved = new();


		public void OnInitializeMain(IMainModCoreAPI api) {
			Instance = this;
			CoreAPI  = api;
			_events = new[] {
				CoreAPI.EventAPI.Subscribe("session_can_make_adapter", Adapting.OnCanMakeAdapter),
				CoreAPI.EventAPI.Subscribe("session_make_adapter", Adapting.OnMakeAdapter)
			};
		}

		public async UniTask OnDisposeMainAsync() {
			foreach (var e in _events)
				CoreAPI.EventAPI.Unsubscribe(e);
			foreach (var connection in Connections)
				await connection.Dispose();
			Connections.Clear();
			CoreAPI  = null;
			Instance = null;
		}

		public void OnUpdateMain() {
			foreach (var connection in Connections)
				connection.Update();
		}

		internal ushort NextId() {
			if (Connections.Count >= ushort.MaxValue)
				return ushort.MaxValue;

			for (var i = 0; i < 1000; i++) {
				var id = (ushort)Random.Range(ushort.MinValue, ushort.MaxValue);
				if (!Connections.Exists(r => r.Id == id))
					return id;
			}

			var fallbackId = ushort.MinValue;
			while (Connections.Exists(r => r.Id == fallbackId))
				fallbackId++;

			return fallbackId;
		}

		public Connection GetByAddress(IPEndPoint endPoint)
			=> Connections.FirstOrDefault(connection => connection.Connector.Remote().Equals(endPoint));

		public Connection GetConnectionByType(string proto) {
			proto = proto.ToLowerInvariant();
			if (TcpConnector.GetStaticProtocolName().Equals(proto))
				return Connection.New<TcpConnector>();
			if (UdpConnector.GetStaticProtocolName().Equals(proto))
				return Connection.New<UdpConnector>();
			return null;
		}
	}
}