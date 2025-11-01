using api.nox.relay.types.Instance;
using api.nox.relay.types.Quit;
using Nox.CCK.Utils;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Leave {
	public class LeaveEvent : RelayInstanceResponse {
		public QuitType Type;
		public ushort   PlayerId;
		public ushort   ByPlayerId;
		public string   Reason;

		public bool HasModerator
			=> ByPlayerId != ushort.MaxValue;

		public override bool FromBuffer(Buffer buffer) {
			buffer.Goto(0);
			InternalId = buffer.ReadByte();
			Type       = buffer.ReadEnum<QuitType>();
			PlayerId   = buffer.ReadUShort();

			// Check if there's additional data for moderation actions
			if (buffer.Remaining > 0) {
				ByPlayerId = buffer.ReadUShort();
				if (buffer.Remaining > 0)
					Reason = buffer.ReadString();
			}

			return true;
		}

		public static LeaveEvent CreateUnknown(ushort cid, byte iid, string reason)
			=> new() {
				ConnectionId = cid,
				InternalId   = iid,
				Type         = QuitType.UnknownError,
				Reason       = reason
			};

		public override string ToString()
			=> $"{GetType().Name}[ConnectionId={ConnectionId}, InternalId={InternalId}, Type={Type}, PlayerId={PlayerId}, ByPlayerId={ByPlayerId}, Reason={Reason}]";
	}
}
