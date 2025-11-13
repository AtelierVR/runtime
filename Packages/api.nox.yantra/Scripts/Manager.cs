using System.Collections.Generic;

namespace api.nox.yantra {
	public class Manager {
		private List<YantraBacking> _backings = new();

		private void InvokeBackingAdded(YantraBacking backing) {
			Main.OnBackingAdded.Invoke(backing);
			Main.Instance.CoreAPI.EventAPI.Emit("yantra_backing_added", backing);
		}

		private void InvokeBackingRemoved(YantraBacking backing) {
			Main.OnBackingRemoved.Invoke(backing);
			Main.Instance.CoreAPI.EventAPI.Emit("yantra_backing_removed", backing);
		}
	}
}

