using Nox.CCK.Utils;

namespace api.nox.relay.types.Disconnect {
	public class RequestDisconnect : RelayRequest {
		public string Reason;

		public override Buffer ToBuffer() {
			var buffer = new Buffer();
			if (!string.IsNullOrEmpty(Reason))
				buffer.Write(Reason);
			return buffer;
		}
	}
}