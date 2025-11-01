using System.Collections.Generic;
using api.nox.relay.types.Instance;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Avatar {
	public class InstanceRequestAvatarParams : RelayInstanceRequest {
		public ushort                  PlayerId;
		public Dictionary<int, byte[]> Parameters;

		public override Buffer ToBuffer() {
			var buffer = new Buffer();
			buffer.Write(InternalId);
			buffer.Write(PlayerId);
			
			buffer.Write((byte)Parameters.Count);
			foreach (var (parameterId, payload) in Parameters)
			{
				buffer.Write(parameterId);
				buffer.Write((ushort)payload.Length);
				buffer.Write(payload);
			}

			return buffer;
		}

		public static InstanceRequestAvatarParams CreateRequest(ushort playerId, Dictionary<int, byte[]> parameters)
			=> new() {
				PlayerId  = playerId,
				Parameters = parameters
			};
	}
}
