using api.nox.relay.types.Instance;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Event {
	public class InstanceRequestEvent : RelayInstanceRequest {
		public const int MaxPayloadSize = 1024;
		public const int MaxTargetCount = 255;
		public const int MaxNameLength  = 32;
		public const int MinNameLength  = 4;

		public string   Name;
		public byte[]   Payload;
		public ushort[] TargetIds;

		public override Buffer ToBuffer() {
			var buffer = new Buffer();
			buffer.Write(InternalId);
			buffer.Write(Name);
			var payload = Payload ?? System.Array.Empty<byte>();
			buffer.Write((ushort)payload.Length);
			buffer.Write(payload);

			if (TargetIds == null)
				return buffer;

			buffer.Write((byte)TargetIds.Length);
			foreach (var targetId in TargetIds)
				buffer.Write(targetId);

			return buffer;
		}

		public static InstanceRequestEvent CreateBroadcast(string name, byte[] payload) {
			if (payload.Length > MaxPayloadSize)
				throw new System.ArgumentException($"Payload size cannot exceed {MaxPayloadSize} bytes.", nameof(payload));
			return name.Length switch {
				> MaxNameLength => throw new System.ArgumentException($"Event name length cannot exceed {MaxNameLength} characters.", nameof(name)),
				< MinNameLength => throw new System.ArgumentException($"Event name length must be at least {MinNameLength} characters.", nameof(name)),
				_               => new InstanceRequestEvent { Name = name, Payload = payload, TargetIds = null }
			};
		}

		public static InstanceRequestEvent CreateTargeted(string name, byte[] payload, ushort[] targetIds) {
			if (payload.Length > MaxPayloadSize)
				throw new System.ArgumentException($"Payload size cannot exceed {MaxPayloadSize} bytes.", nameof(payload));
			return targetIds.Length switch {
				0                => throw new System.ArgumentException("Target IDs cannot be empty for a targeted event.", nameof(targetIds)),
				> MaxTargetCount => throw new System.ArgumentException($"Target count cannot exceed {MaxTargetCount}.", nameof(targetIds)),
				_ => name.Length switch {
					> MaxNameLength => throw new System.ArgumentException($"Event name length cannot exceed {MaxNameLength} characters.", nameof(name)),
					< MinNameLength => throw new System.ArgumentException($"Event name length must be at least {MinNameLength} characters.", nameof(name)),
					_               => new InstanceRequestEvent { Name = name, Payload = payload, TargetIds = targetIds }
				}
			};
		}

		public override string ToString()
			=> $"{GetType().Name}[InternalId={InternalId}, Name={Name}, PayloadLength={Payload?.Length ?? 0}, TargetCount={(TargetIds == null ? "Broadcast" : TargetIds.Length.ToString())}]";
	}
}