using api.nox.relay.types;
using Nox.CCK.Utils;

namespace api.nox.relay.types.Disconnect {
	public class RelayEventDisconnect : RelayResponse {
		public string Reason;

		public override bool FromBuffer(Buffer buffer) {
			buffer.Goto(0);
			if (buffer.Remaining > 0) // Check if there is reason data
				Reason = buffer.ReadString();
			return true;
		}
	}
}