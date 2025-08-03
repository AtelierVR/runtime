using System;
using api.nox.relay.types.Instance;
using api.nox.relay.types.Player;
using Nox.CCK.Utils;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Join {
	public class JoinEvent : RelayInstanceResponse {
		public InstancePlayer Player;
		public string         Engine;
		public string         Platform;

		public override bool FromBuffer(Buffer buffer) {
			buffer.Goto(0);
			InternalId = buffer.ReadByte();
			
			Player = new InstancePlayer {
				ConnectionId = ConnectionId,
				InternalId   = InternalId,
				Flags        = buffer.ReadEnum<InstancePlayerFlags>(),
				Id           = buffer.ReadUShort(),
				Identifier   = Main.UserAPI.Make(buffer.ReadUInt(), buffer.ReadString()),
				Display      = buffer.ReadString(),
				JoinedAt     = buffer.ReadDateTime()
			};
			
			Engine   = buffer.ReadString();
			Platform = buffer.ReadString();
			
			return true;
		}

		public static JoinEvent CreateUnknown(ushort cid, byte iid, string reason)
			=> new() {
				ConnectionId = cid,
				InternalId   = iid,
				Player = new InstancePlayer {
					ConnectionId = cid,
					InternalId   = iid,
					Display      = reason
				}
			};

		public override string ToString()
			=> $"{GetType().Name}[ConnectionId={ConnectionId}, InternalId={InternalId}, Player={Player?.ToString() ?? "null"}, Engine={Engine}, Platform={Platform}]";
	}
}
