using Nox.CCK.Utils;

namespace api.nox.relay.types {
	public abstract class RelayRequest : INoxObject {
		public          ushort ConnectionId { get; set; }
		public abstract Buffer ToBuffer();
	}
}