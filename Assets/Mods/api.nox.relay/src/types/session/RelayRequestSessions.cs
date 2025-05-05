using Nox.CCK.Utils;

namespace api.nox.relay.types.Session {
	public class RelayRequestSessions : RelayRequest {
		public byte Page;

		public override Buffer ToBuffer() {
			var buffer = new Buffer();
			if (Page != 0) buffer.Write(Page);
			return buffer;
		}
	}
}