using Nox.CCK.Utils;

namespace api.nox.relay.Instances {
	public class RelayInstance : INoxObject {
		public uint          Id;
		public byte          InternalId;
		public ushort        ConnectionId;
		public ushort        PlayerCount;
		public ushort        MaxPlayerCount;
		public InstanceFlags Flags;

		public override string ToString()
			=> $"{GetType().Name}[Id={Id}, InternalId={InternalId}, ConnectionId={ConnectionId}, PlayerCount={PlayerCount}, MaxPlayerCount={MaxPlayerCount}, Flags={Flags}]";
	}
}