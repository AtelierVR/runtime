using System.Collections.Generic;
using api.nox.relay.types.Instance;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Properties {
	public class PropertiesEvent : RelayInstanceResponse {
		public          ushort                  ByEntityId;
		public          ushort                  EntityId;
		public readonly Dictionary<int, byte[]> Parameters = new();

		public override bool FromBuffer(Buffer buffer) {
			buffer.Goto(0);

			InternalId = buffer.ReadByte();
			EntityId   = buffer.ReadUShort();
			ByEntityId = buffer.ReadUShort();
			var parameterCount = buffer.ReadByte();

			for (var i = 0; i < parameterCount; i++) {
				var parameterId = buffer.ReadInt();
				var payloadSize = buffer.ReadByte();
				var payload     = buffer.ReadBytes(payloadSize);
				Parameters[parameterId] = payload;
			}


			return true;
		}

		public override string ToString()
			=> $"{GetType().Name}[ConnectionId={ConnectionId}, InternalId={InternalId}, EntityId={EntityId}, ParametersCount={Parameters.Count}, ByEntityId={ByEntityId}]";
	}
}