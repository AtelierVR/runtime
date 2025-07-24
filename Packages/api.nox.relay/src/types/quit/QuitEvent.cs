using api.nox.relay.types.Enter;
using api.nox.relay.types.Instance;
using Nox.CCK.Utils;

namespace api.nox.relay.types.Quit {
	public class QuitEvent : RelayInstanceResponse {
		public QuitType Type;
		public string   Reason;

		public override bool FromBuffer(Buffer buffer) {
			buffer.Goto(0);
			InternalId = buffer.ReadByte();
			Type       = buffer.ReadEnum<QuitType>();
			if (buffer.Remaining > 2) // Check if there is reason data
				Reason = buffer.ReadString();
			return true;
		}

		public static QuitEvent CreateUnknown(ushort cid, byte iid, string reason)
			=> new() {
				ConnectionId = cid,
				InternalId   = iid,
				Type         = QuitType.UnknownError,
				Reason       = reason
			};

		public override string ToString()
			=> $"{GetType().Name}[ConnectionId={ConnectionId}, InternalId={InternalId}, Type={Type}, Reason={Reason}]";
	}
}