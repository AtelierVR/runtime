using System.Collections.Generic;

namespace api.nox.relay.types.Session {
	public class RelayResponseSessions : RelayResponse {
		public List<RelayInstance> Instances = new();
		public byte                Page;
		public byte                PageCount;

		public override bool FromBuffer(Utils.Buffer buffer) {
			Flags         = buffer.ReadEnum<RelayFlags>();
			MasterAddress = buffer.ReadString();
			var instanceCount = buffer.ReadByte();
			for (var i = 0; i < instanceCount; i++)
				Instances.Add(
					new RelayInstance {
						RelayId        = RelayId,
						Flags          = buffer.ReadEnum<InstanceFlags>(),
						InternalId     = buffer.ReadUShort(),
						Id             = buffer.ReadUInt(),
						PlayerCount    = buffer.ReadUShort(),
						MaxPlayerCount = buffer.ReadUShort(),
					}
				);
			Page      = buffer.ReadByte();
			PageCount = buffer.ReadByte();
			return true;
		}

		public override string ToString()
			=> $"{GetType().Name}[Flags={Flags}, MasterAddress={MasterAddress}, Instances={Instances.Count}, Page={Page}/{PageCount}]";
	}
}