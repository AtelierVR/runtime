using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;

namespace api.nox.controller {
	public class ControllerClient : ClientModInitializer {
		public void OnInitializeClient(ClientModCoreAPI api) {
			if (DesktopController.IsBetterThanCurrent())
				DesktopController.Make();
		}
	}
}