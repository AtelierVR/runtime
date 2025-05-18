using Nox.CCK.Utils;

namespace api.nox.relay.Instances {
	public class RelayInstance : INoxObject {
		public uint          Id;
		public ushort        InternalId;
		public ushort        ConnectionId;
		public ushort        PlayerCount;
		public ushort        MaxPlayerCount;
		public InstanceFlags Flags;
	}
}