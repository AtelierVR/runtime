using Nox.Avatars;
using Nox.CCK.Utils;
using NoxTransform = Nox.CCK.Utils.Transform;

namespace api.nox.relay {
	public class RelayPhysicalLocalPlayer : RelayPhysicalPlayer {
		public override IRuntimeAvatar GetAvatar()
			=> RelayLocalPlayer.TryCurrentController(out var controller)
				? controller.GetAvatar()
				: null;

		public override void OnMove(ushort part, NoxTransform move) {
			Logger.LogWarning($"Received OnMove for local player on part {part}, which is not supported.", this);
		}

		public override void OnParameter(int key, byte[] value) {
			Logger.LogWarning($"Received OnParameter for local player on key {key}, which is not supported.", this);
		}
	}
}