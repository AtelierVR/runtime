using api.nox.relay.types.Instance;
using Nox.CCK.Utils;

namespace api.nox.relay.types.PlayerUpdate {
	public class RequestPlayerUpdate : RelayInstanceRequest {
		public byte              PlayerId;
		public PlayerUpdateFlags Flags;
		public string            DisplayName;
		public uint              PlayerFlags;

		public override Buffer ToBuffer() {
			var buffer = new Buffer();
			buffer.Write(InternalId);
			buffer.Write(PlayerId);
			buffer.Write(Flags);

			if (Flags.HasFlag(PlayerUpdateFlags.DisplayName) && !string.IsNullOrEmpty(DisplayName)) {
				buffer.Write(DisplayName);
			}

			if (Flags.HasFlag(PlayerUpdateFlags.Flags)) {
				buffer.Write(PlayerFlags);
			}

			return buffer;
		}
	}
}