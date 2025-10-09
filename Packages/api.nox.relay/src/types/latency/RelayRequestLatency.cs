using System;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Latency {
	public class RelayRequestLatency : RelayRequest {
		public const float IntervalLatencyRequest = 2.5f;

		public DateTime InitialTime;

		public override Buffer ToBuffer() {
			var buffer = new Buffer();
			buffer.Write(InitialTime);
			return buffer;
		}

		public static RelayRequestLatency Now(ushort id)
			=> new() {
				ConnectionId = id,
				InitialTime  = DateTime.UtcNow
			};
	}
}