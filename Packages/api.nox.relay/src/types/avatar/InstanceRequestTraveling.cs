using api.nox.relay.types.Instance;
using Nox.CCK.Utils;

namespace api.nox.relay.types.Avatar {
	public class InstanceRequestAvatarChanged : RelayInstanceRequest {
		public AvatarChangedAction Action;
		public string              Reason;

		public override Buffer ToBuffer() {
			var buffer = new Buffer();
			buffer.Write(InternalId);
			buffer.Write(Action);
			if (Action == AvatarChangedAction.Failed && !string.IsNullOrEmpty(Reason))
				buffer.Write(Reason);
			return buffer;
		}
	}
}