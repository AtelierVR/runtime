using System.Threading.Tasks;
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
		public byte          Tps = byte.MaxValue;

		public Connection Connection;

		public readonly UnityEvent<types.Traveling.TravelingEvent>  OnTraveling     = new();
		public readonly UnityEvent<types.Quit.QuitEvent>            OnQuit          = new();
		public readonly UnityEvent<types.Enter.EnterResponse>       OnEnter         = new();
		public readonly UnityEvent<types.Join.JoinEvent>            OnJoin          = new();
		public readonly UnityEvent<types.Leave.LeaveEvent>          OnLeave         = new();
		public readonly UnityEvent<types.Avatar.AvatarChangedEvent> OnAvatarChanged = new();
		public readonly UnityEvent<types.Transform.TransformEvent>  OnTransform     = new();
		public readonly UnityEvent<types.Voice.VoiceEvent>          OnVoice         = new();
		public readonly UnityEvent<types.Avatar.AvatarParamsEvent>  OnAvatarParams  = new();

		internal async UniTask OnReceived(ushort length, ushort state, ResponseType type, Buffer buffer) {
			await UniTask.SwitchToMainThread();
			buffer.Goto(0);
			switch (type) {
				case ResponseType.Quit:
					var quit = new types.Quit.QuitEvent { ConnectionId = Connection.Id, InternalId = InternalId };
					if (quit.FromBuffer(buffer)) OnQuit.Invoke(quit);
					else Logger.LogWarning($"Failed to parse quit event for instance {InternalId}");
					break;
				case ResponseType.Traveling:
					var traveling = new types.Traveling.TravelingEvent { ConnectionId = Connection.Id, InternalId = InternalId };
					if (traveling.FromBuffer(buffer)) OnTraveling.Invoke(traveling);
					else Logger.LogWarning($"Failed to parse traveling event for instance {InternalId}");
					break;
				case ResponseType.Enter:
					var enter = new types.Enter.EnterResponse { ConnectionId = Connection.Id, InternalId = InternalId };
					if (enter.FromBuffer(buffer)) OnEnter.Invoke(enter);
					else Logger.LogWarning($"Failed to parse enter response for instance {InternalId}");
					break;
				case ResponseType.Join:
					var join = new types.Join.JoinEvent { ConnectionId = Connection.Id, InternalId = InternalId };
					if (join.FromBuffer(buffer)) OnJoin.Invoke(join);
					else Logger.LogWarning($"Failed to parse join event for instance {InternalId}");
					break;
				case ResponseType.Leave:
					var leave = new types.Leave.LeaveEvent { ConnectionId = Connection.Id, InternalId = InternalId };
					if (leave.FromBuffer(buffer)) OnLeave.Invoke(leave);
					else Logger.LogWarning($"Failed to parse leave event for instance {InternalId}");
					break;
				case ResponseType.AvatarChanged:
					var avatar = new types.Avatar.AvatarChangedEvent { ConnectionId = Connection.Id, InternalId = InternalId };
					if (avatar.FromBuffer(buffer)) OnAvatarChanged.Invoke(avatar);
					else Logger.LogWarning($"Failed to parse avatar changed event for instance {InternalId}");
					break;
				case ResponseType.AvatarParams:
					var avatarParams = new types.Avatar.AvatarParamsEvent { ConnectionId = Connection.Id, InternalId = InternalId };
					if (avatarParams.FromBuffer(buffer)) OnAvatarParams.Invoke(avatarParams);
					else Logger.LogWarning($"Failed to parse avatar params event for instance {InternalId}");
					break;
				case ResponseType.Transform:
					var transform = new types.Transform.TransformEvent { ConnectionId = Connection.Id, InternalId = InternalId };
					if (transform.FromBuffer(buffer)) OnTransform.Invoke(transform);
					else Logger.LogWarning($"Failed to parse transform event for instance {InternalId}");
					break;
				case ResponseType.Voice:
					var voice = new types.Voice.VoiceEvent { ConnectionId = Connection.Id, InternalId = InternalId };
					if (voice.FromBuffer(buffer)) OnVoice.Invoke(voice);
					else Logger.LogWarning($"Failed to parse voice event for instance {InternalId}");
					break;
				default:
					Logger.LogDebug($"Received unknown response type {type} for instance {InternalId}");
					break;
			}
		}

		public async UniTask<bool> SendTransform(types.Transform.InstanceRequestTransform request) {
			request.InternalId   = InternalId;
			request.ConnectionId = Connection.Id;
			return (await Connection.Emit(request.ToBuffer(), RequestType.Transform)).Item1;
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

		public async UniTask<types.Avatar.AvatarChangedEvent> RequestAvatarChange(types.Avatar.InstanceRequestAvatarChanged request) {
			request.InternalId   = InternalId;
			request.ConnectionId = Connection.Id;
			return await Connection.Request<types.Avatar.AvatarChangedEvent>(
					request,
					RequestType.AvatarChanged,
					ResponseType.AvatarChanged,
					Connection.NextState()
				)
				?? types.Avatar.AvatarChangedEvent.CreateUnknown(Connection.Id, InternalId, "Unknown avatar change request");
		}

		public async UniTask<bool> SendAvatarParams(types.Avatar.InstanceRequestAvatarParams request) {
			request.InternalId   = InternalId;
			request.ConnectionId = Connection.Id;
			return (await Connection.Emit(request.ToBuffer(), RequestType.AvatarParams)).Item1;
		}

		public async UniTask<bool> SendVoice(types.Voice.InstanceRequestVoice request) {
			request.InternalId   = InternalId;
			request.ConnectionId = Connection.Id;
			if (request.IsEmpty())
				Logger.LogWarning("Sending empty voice packet");
			return (await Connection.Emit(request.ToBuffer(), RequestType.Voice)).Item1;
		}
	}
}