using api.nox.relay.types.Instance;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Event {
	public class InstanceRequestEvent : RelayInstanceRequest {
		public string   Name;
		public byte[]   Payload;
		public ushort[] TargetIds;

		public override Buffer ToBuffer() {
			var buffer = new Buffer();
			buffer.Write(InternalId);
			buffer.Write(Name);
			buffer.Write((ushort)Payload.Length);
			buffer.Write(Payload);

			if (TargetIds == null)
				return buffer;

			buffer.Write((byte)TargetIds.Length);
			foreach (var targetId in TargetIds)
				buffer.Write(targetId);

			return buffer;
		}

		public static InstanceRequestEvent CreateBroadcast(string name, byte[] payload)
			=> new() {
				Name      = name,
				Payload   = payload,
				TargetIds = null
			};

		public static InstanceRequestEvent CreateTargeted(string name, byte[] payload, ushort[] targetIds)
			=> new() {
				Name      = name,
				Payload   = payload,
				TargetIds = targetIds
			};

		public override string ToString()
			=> $"{GetType().Name}[InternalId={InternalId}, Name={Name}, PayloadLength={Payload.Length}, TargetCount={(TargetIds == null ? "Broadcast" : TargetIds.Length.ToString())}]";
	}
}