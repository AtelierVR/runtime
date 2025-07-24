using api.nox.relay.types.Instance;
using Nox.CCK.Utils;

namespace api.nox.relay.types.Traveling {
	public class InstanceRequestTraveling : RelayInstanceRequest {
		public TravelingAction Action;
		public string          Reason;

		public override Buffer ToBuffer() {
			var buffer = new Buffer();
			buffer.Write(InternalId);
			buffer.Write(Action);
			if (Action == TravelingAction.Failed && !string.IsNullOrEmpty(Reason))
				buffer.Write(Reason);
			return buffer;
		}
	}
}