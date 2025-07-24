using api.nox.relay.connection;
using api.nox.relay.types;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine.Events;

namespace api.nox.relay.Instances {
	public class RelayInstance : INoxObject {
		public uint          MasterId;
		public byte          InternalId;
		public ushort        PlayerCount;
		public ushort        MaxPlayerCount;
		public InstanceFlags Flags;

		public Connection Connection;

		public UnityEvent<types.Traveling.TravelingEvent> OnTraveling = new();
		public UnityEvent<types.Quit.QuitEvent>           OnQuit      = new();
		public UnityEvent<types.Enter.EnterResponse>      OnEnter     = new();

		internal void OnReceived(ushort length, ushort state, ResponseType type, Buffer buffer) {
			buffer.Goto(0);
			switch (type) {
				case ResponseType.Quit:
					var quit = new types.Quit.QuitEvent { ConnectionId = Connection.Id, InternalId = InternalId };
					if (quit.FromBuffer(buffer)) OnQuit.Invoke(quit);
					break;
				case ResponseType.Traveling:
					var traveling = new types.Traveling.TravelingEvent { ConnectionId = Connection.Id, InternalId = InternalId };
					if (traveling.FromBuffer(buffer)) OnTraveling.Invoke(traveling);
					break;
				case ResponseType.Enter:
					var enter = new types.Enter.EnterResponse { ConnectionId = Connection.Id, InternalId = InternalId };
					if (enter.FromBuffer(buffer)) OnEnter.Invoke(enter);
					break;
				default:
					Logger.LogDebug($"Received unknown response type {type} for instance {InternalId}");
					break;
			}
		}

		public async UniTask<types.Enter.EnterResponse> RequestEnter(string display = null, string password = null, types.Enter.EnterFlags flags = types.Enter.EnterFlags.None) {
			Connection.Instances.Add(this);

			var enter = await Connection.Request<types.Enter.EnterResponse>(
				new types.Enter.InstanceRequestEnter {
					ConnectionId = Connection.Id,
					InternalId   = InternalId,
					Display      = display,
					Password     = password,
					Flags        = flags
				},
				RequestType.Enter,
				ResponseType.Enter,
				Connection.NextState()
			);

			enter ??= types.Enter.EnterResponse.CreateUnknown(Connection.Id, InternalId, "Unknown enter request");

			if (enter.IsError) {
				Connection.Instances.Remove(this);
				return enter;
			}

			return enter;
		}

		public async UniTask<types.Quit.QuitEvent> RequestQuit(types.Quit.QuitType type = types.Quit.QuitType.Normal, string reason = null) {
			var quit = await Connection.Request<types.Quit.QuitEvent>(
				new types.Quit.InstanceRequestQuit {
					ConnectionId = Connection.Id,
					InternalId   = InternalId,
					Type         = type,
					Reason       = reason
				},
				RequestType.Quit,
				ResponseType.Quit,
				Connection.NextState()
			);

			quit ??= types.Quit.QuitEvent.CreateUnknown(Connection.Id, InternalId, "Unknown quit request");
			Connection.Instances.Remove(this);
			return quit;
		}

		public override string ToString()
			=> $"{GetType().Name}[Id={MasterId}, InternalId={InternalId}, ConnectionId={Connection.Id}, PlayerCount={PlayerCount}, MaxPlayerCount={MaxPlayerCount}, Flags={Flags}]";

		public async UniTask<types.Traveling.TravelingEvent> RequestTraveling(types.Traveling.TravelingAction action, string reason = null)
			=> await Connection.Request<types.Traveling.TravelingEvent>(
					new types.Traveling.InstanceRequestTraveling {
						ConnectionId = Connection.Id,
						InternalId   = InternalId,
						Action       = action,
						Reason       = reason
					},
					RequestType.Traveling,
					ResponseType.Traveling,
					Connection.NextState()
				)
				?? types.Traveling.TravelingEvent.CreateUnknown(Connection.Id, InternalId, "Unknown traveling request");
	}
}