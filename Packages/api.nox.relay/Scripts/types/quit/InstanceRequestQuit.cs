using api.nox.relay.types.Instance;
using Nox.CCK.Utils;

namespace api.nox.relay.types.Quit {
	public class InstanceRequestQuit : RelayInstanceRequest {
		public QuitType Type;
		public string   Reason;

		public override Buffer ToBuffer() {
			var buffer = new Buffer();
			buffer.Write(InternalId);
			buffer.Write(Type);
			if (!string.IsNullOrEmpty(Reason))
				buffer.Write(Reason);
			return buffer;
		}
	}
}