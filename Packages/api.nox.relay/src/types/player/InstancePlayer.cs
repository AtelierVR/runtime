using System;

namespace api.nox.relay.types.Player {
	public class InstancePlayer {
		public byte   InternalId;
		public ushort ConnectionId;

		public InstancePlayerFlags Flags;
		public ushort              Id;
		public uint                MasterId;
		public string              ServerAddress;
		public string              Display;
		public DateTime            CreatedAt;
	}
}