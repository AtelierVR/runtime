using Nox.CCK.Utils;

namespace api.nox.relay.types {
	public abstract class RelayRequest : INoxObject {
		public          ushort ConnectionId;
		public abstract Buffer ToBuffer();
	}
}