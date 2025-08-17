using Nox.Avatars;

namespace api.nox.relay {
	public class RelayPhysicalLocalPlayer : RelayPhysicalPlayer {
		public override IAvatar GetAvatar()
			=> Main.ControllerAPI.GetCurrent()?.GetAvatar();
	}
}