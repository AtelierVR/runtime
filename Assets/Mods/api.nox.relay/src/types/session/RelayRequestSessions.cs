using Nox.CCK.Utils;

namespace api.nox.relay.types.Session {
	public class RequestSessions : Request {
		public byte Page;

		public override Buffer ToBuffer() {
			var buffer = new Buffer();
			if (Page != 0) buffer.Write(Page);
			return buffer;
		}
	}
}