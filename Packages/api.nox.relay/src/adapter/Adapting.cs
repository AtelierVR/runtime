using System;
using System.Collections.Generic;
using System.Net;
using api.nox.relay.connection;
using api.nox.relay.types.Authentication;
using api.nox.relay.types.Traveling;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Events;
using Nox.CCK.Utils;
using Nox.Sessions;

namespace api.nox.relay {
	public class Adapting {
		public static void OnCanMakeAdapter(EventData context) {
			if (!context.TryGet<string>(0, out var type) || type is not "external:relay") return;
			var options = !context.TryGet<Dictionary<string, object>>(1, out var opts)
				? new Dictionary<string, object>()
				: opts;

			// verify that connections is string[]
			if (!options.TryGetValue("connections", out var connectionsObj) || connectionsObj is not string[] connections) {
				Logger.LogError("Relay adapter requires 'connections' to be set in options as string[]");
				return;
			}

			// verify that elements is <proto>://<address>:<port>
			var regex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z][a-zA-Z0-9+.-]*://[a-zA-Z0-9.-]+:\d+$");
			foreach (var connection in connections)
				if (!regex.IsMatch(connection)) {
					Logger.LogError($"Relay adapter requires 'connections' to be set in options as string[] with elements in format <proto>://<address>:<port>, got '{connection}'");
					return;
				}

			if (connections.Length == 0) {
				Logger.LogError("Relay adapter requires 'connections' to be set in options as string[] with at least one connection");
				return;
			}

			// verify that address is string
			if (!options.TryGetValue("address", out var addressObj) || addressObj is not string) {
				Logger.LogError("Relay adapter requires 'address' to be set in options as string");
				return;
			}

			// verify that server is string
			if (!options.TryGetValue("server", out var serverObj) || serverObj is not string) {
				Logger.LogError("Relay adapter requires 'server' to be set in options as string");
				return;
			}

			// verify that instance is uint
			if (!options.TryGetValue("instance", out var instanceObj) || instanceObj is not uint) {
				Logger.LogError("Relay adapter requires 'instance' to be set in options as uint");
				return;
			}

			context.Callback(true);
		}

		public static void OnMakeAdapter(EventData context) {
			if (!context.TryGet<string>(0, out var type) || type is not "external:relay") return;

			var options = !context.TryGet<Dictionary<string, object>>(1, out var opts)
				? new Dictionary<string, object>()
				: opts;
			var connections = options.TryGetValue("connections", out var l0)
				&& l0 is string[] l1
					? l1
					: Array.Empty<string>();
			var address = options.TryGetValue("address", out var a0)
				&& a0 is string a1
					? a1
					: null;
			var setCurrent = !options.TryGetValue("set_current", out var c0)
				|| c0 is not bool c1
				|| c1;
			var server = options.TryGetValue("server", out var s0)
				&& s0 is string s1
					? s1
					: null;
			var instance = options.TryGetValue("instance", out var i0)
				&& i0 is uint i1
					? i1
					: 0u;

			var adapter = new RelayAdapter();
			var session = Main.SessionAPI.New(adapter);
			adapter.SetState(false, "Preparing offline session...", 0f);
			context.Callback(adapter, session);
			PrepareAsync(session, adapter, connections, address, server, instance, setCurrent).Forget();
		}

		private static async UniTask<(string, IPEndPoint)> ParseIPEndPoint(string address) {
			var uri     = new Uri(address);
			var uriType = Uri.CheckHostName(uri.Host);

			switch (uriType) {
				case UriHostNameType.IPv4 or UriHostNameType.IPv6:
					return (uri.Scheme, new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port));
				case UriHostNameType.Dns: {
					var ip = await Dns.GetHostAddressesAsync(uri.Host);
					if (ip.Length > 0)
						return (uri.Scheme, new IPEndPoint(ip[0], uri.Port));
					break;
				}
			}

			return (null, null);
		}

		private static async UniTask PrepareAsync(ISession session, RelayAdapter adapter, string[] connections, string address, string server, uint instance, bool setCurrent) {
			adapter.SetState(false, "Fetching token...", 0.05f);
			var token = await Main.UserAPI.GetToken(server);
			if (token == null) {
				adapter.SetState(false, "Failed to fetch token", -1f);
				return;
			}

			Connection connection = null;
			foreach (var addr in connections) {
				if (connection != null)
					await connection.Dispose();

				adapter.SetState(false, $"Connecting to {addr}...", 0.1f);
				var (proto, endPoint) = await ParseIPEndPoint(addr);

				connection = Main.Instance.GetConnectionByType(proto);
				if (connection == null) {
					Logger.LogWarning($"No connection for protocol {proto} found, trying to create a new one");
					continue;
				}

				if (!await connection.Connect(endPoint.Address.ToString(), (ushort)endPoint.Port)) {
					Logger.LogWarning($"Failed to connect to {addr}");
					continue;
				}

				break;
			}


			if (connection == null) {
				adapter.SetState(false, "Failed to connect to any relay", -1f);
				return;
			}

			adapter.Connection = connection;

			adapter.SetState(false, "Handshaking with relay...", 0.2f);
			var hand = await connection.RequestHandshake();
			if (hand == null) {
				adapter.SetState(false, "Failed to handshake with relay", -1f);
				await connection.Dispose();
				return;
			}

			adapter.SetState(false, "Authenticating with relay...", 0.225f);
			var request = RelayRequestAuthentication.CreateAuth(token);
			var auth    = await connection.RequestAuthentication(request);
			if (auth.IsError) {
				adapter.SetState(false, $"Authentication failed: {auth.Reason}", -1f);
				await connection.Dispose();
				return;
			}

			adapter.SetState(false, "Fetching instances from relay...", 0.25f);
			adapter.Instance = await connection.RequestSession(instance);
			if (adapter.Instance == null) {
				adapter.SetState(false, $"Failed to get instance {instance}", -1f);
				await connection.Dispose();
				return;
			}

			adapter.Instance.OnQuit.AddListener(adapter.OnQuit);
			adapter.Instance.OnJoin.AddListener(adapter.OnJoin);
			adapter.Instance.OnLeave.AddListener(adapter.OnLeave);
			adapter.Instance.OnAvatarChanged.AddListener(adapter.OnAvatarChanged);
			adapter.Instance.OnTransform.AddListener(adapter.OnTransform);

			adapter.SetState(false, "Connecting to an instance...", 0.3f);
			var enter = await adapter.Instance.RequestEnter();
			if (enter.IsError) {
				adapter.SetState(false, "Failed to connect to instance", -1f);
				Logger.LogDebug($"Failed to connect to instance {instance}: {enter.Result} - {enter.Reason}");
				await connection.Dispose();
				return;
			}

			adapter.Tps       = enter.Tps;
			adapter.Threshold = enter.Threshold;
			
			adapter.SetState(false, $"Connected as {enter.Player.Display} ({enter.Player.Id}) to instance {instance}", 0.325f);

			var travalRequest = await adapter.Instance.RequestTraveling(TravelingAction.Travel);
			if (travalRequest.IsError) {
				adapter.SetState(false, $"Failed to travel to instance {instance}: {travalRequest.Reason}", -1f);
				Logger.LogDebug($"Failed to travel to instance {instance}: {travalRequest.Results} - {travalRequest.Reason}");
				await connection.Dispose();
				return;
			}

			var travaling = await adapter.OnTravelingAsync(
				travalRequest,
				autoResponse: false,
				progress: (f, s) => adapter.SetState(false, $"Traveling to instance {instance}...", 0.325f + f * 0.575f)
			);


			if (!travaling) {
				adapter.SetState(false, "Failed to travel to instance", -1f);
				Logger.LogDebug($"Failed to travel to instance {instance}");
				await connection.Dispose();
				return;
			}

			var travelReady = await adapter.Instance.RequestTraveling(TravelingAction.Ready);
			if (!travelReady.IsReady) {
				adapter.SetState(false, $"Failed to travel to instance {instance}: {travelReady.Reason}", -1f);
				Logger.LogDebug($"Failed to travel to instance {instance}: {travelReady.Results} - {travelReady.Reason}");
				await connection.Dispose();
				return;
			}

			adapter.Instance.OnTraveling.AddListener(adapter.OnTraveling);
			adapter.Instance.OnEnter.AddListener(adapter.OnEnter);

			var player = adapter.NewPlayer<RelayLocalPlayer>(enter.Player);

			adapter.SetState(false, "Setting local player avatar...", 0.9f);
			if (!await player.SetAvatar(player.GetAvatar())) {
				adapter.SetState(false, "Failed to set local player avatar", -1f);
				Logger.LogDebug($"Failed to set local player avatar for instance {instance}");
				await connection.Dispose();
				return;
			}

			if (setCurrent) {
				adapter.SetState(false, "Setting instance as current", 0.95f);
				await session.SetCurrent();
			}

			adapter.SetState(true, "Ready", 1f);
		}
	}
}