using api.nox.relay.types.Instance;
using api.nox.relay.types.Player;
using Nox.CCK.Utils;

namespace api.nox.relay.types.PlayerUpdate {
	public class PlayerUpdateEvent : RelayInstanceResponse {
		public ushort              PlayerId;
		public PlayerUpdateResult  Result;
		public string              Reason;
		public PlayerUpdateFlags   Flags;
		public string              DisplayName;
		public InstancePlayerFlags PlayerFlags;

		public override bool FromBuffer(Buffer buffer) {
			buffer.Goto(0);
			InternalId = buffer.ReadByte();
			Result     = buffer.ReadEnum<PlayerUpdateResult>();
			
			if (Result == PlayerUpdateResult.Failure) {
				Reason = buffer.Remaining > 2 ? buffer.ReadString() : "Unknown reason";
				return true;
			}

			PlayerId = buffer.ReadUShort();
			Flags    = buffer.ReadEnum<PlayerUpdateFlags>();

			if (Flags.HasFlag(PlayerUpdateFlags.DisplayName))
				DisplayName = buffer.ReadString();

			if (Flags.HasFlag(PlayerUpdateFlags.Flags))
				PlayerFlags = buffer.ReadEnum<InstancePlayerFlags>();

			return true;
		}

		public static PlayerUpdateEvent CreateFailure(ushort cid, byte iid, string message)
			=> new() {
				ConnectionId = cid,
				InternalId   = iid,
				Result       = PlayerUpdateResult.Failure,
				Reason       = message
			};
	}
}