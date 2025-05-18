using api.nox.relay.types;
using Nox.CCK.Utils;

namespace api.nox.relay.Disconnect {
	public class RelayEventDisconnect : RelayResponse {
		public string Reason;

		public override bool FromBuffer(Buffer buffer) {
			if (buffer.length > 0)
				Reason = buffer.ReadString();
			return true;
		}
	}
}