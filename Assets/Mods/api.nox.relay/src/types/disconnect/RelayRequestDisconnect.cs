using api.nox.relay.types;
using Nox.CCK.Utils;

namespace api.nox.relay.Disconnect {
	public class RelayRequestDisconnect : RelayRequest {
		public string Raison;

		public override Buffer ToBuffer() {
			var buffer = new Buffer();
			if (!string.IsNullOrEmpty(Raison))
				buffer.Write(Raison);
			return buffer;
		}
	}
}