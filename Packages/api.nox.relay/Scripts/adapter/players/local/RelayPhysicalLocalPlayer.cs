using Nox.Avatars;

namespace api.nox.relay {
	public class RelayPhysicalLocalPlayer : RelayPhysicalPlayer {
		public override IRuntimeAvatar GetAvatar()
			=> RelayExtensions.TryCurrentController(out var controller)
				? controller.GetAvatar()
				: null;
	}
}