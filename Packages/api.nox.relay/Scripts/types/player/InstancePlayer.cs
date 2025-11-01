using System;
using Nox.Users;

namespace api.nox.relay.types.Player {
	public class InstancePlayer {
		public byte   InternalId;
		public ushort ConnectionId;

		public InstancePlayerFlags Flags;
		public ushort              Id;
		public IUserIdentifier     Identifier;
		public string              Display;
		public DateTime            JoinedAt;
	}
}