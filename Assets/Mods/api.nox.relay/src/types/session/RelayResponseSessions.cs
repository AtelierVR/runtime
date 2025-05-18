using api.nox.relay.Instances;
using Nox.CCK.Utils;

namespace api.nox.relay.types.Session {
	public class RelayResponseSessions : RelayResponse {
		public RelayInstance[] Instances;
		public byte            Page;
		public byte            PageCount;

		public override bool FromBuffer(Buffer buffer) {
			var instanceCount = buffer.ReadByte();
			var instances     = new RelayInstance[instanceCount];
			for (var i = 0; i < instanceCount; i++)
				instances[i] = new RelayInstance {
					ConnectionId   = ConnectionId,
					Flags          = buffer.ReadEnum<InstanceFlags>(),
					InternalId     = buffer.ReadUShort(),
					Id             = buffer.ReadUInt(),
					PlayerCount    = buffer.ReadUShort(),
					MaxPlayerCount = buffer.ReadUShort(),
				};
			Page      = buffer.ReadByte();
			PageCount = buffer.ReadByte();
			Instances = instances;
			return true;
		}

		public override string ToString()
			=> $"{GetType().Name}[Instances={Instances.Length}, Page={Page}/{PageCount}]";
	}
}