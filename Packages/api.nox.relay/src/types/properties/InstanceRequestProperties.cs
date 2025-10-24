using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.relay.types.Instance;
using Nox.CCK.Network;
using Nox.Entities;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Properties {
	public class InstanceRequestProperties : RelayInstanceRequest {
		public ushort                     EntityId;
		public Dictionary<int, byte[]> Parameters;

		public override Buffer ToBuffer() {
			var buffer = new Buffer();
			buffer.Write(InternalId);
			buffer.Write(EntityId);
			buffer.Write((byte)Parameters.Count);

			foreach (var parameter in Parameters) {
				buffer.Write(parameter.Key);
				buffer.Write((ushort)parameter.Value.Length);
				buffer.Write(parameter.Value);
			}

			return buffer;
		}

		public static InstanceRequestProperties CreateRequest(ushort playerId, IProperty[] parameters) {
			if (parameters.Length == byte.MaxValue)
				throw new ArgumentException($"Cannot have more than {byte.MaxValue} parameters in an property request.", nameof(parameters));
			return new InstanceRequestProperties {
				EntityId   = playerId,
				Parameters = parameters.ToDictionary(p => p.GetKey().Hash(), p => p.Serialize())
			};
		}
	}
}