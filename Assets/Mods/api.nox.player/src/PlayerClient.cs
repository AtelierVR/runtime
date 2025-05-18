using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;

namespace api.nox.player {
	public class PlayerClient : ClientModInitializer {
		public void OnInitializeClient(ClientModCoreAPI api) {
			if (DesktopProxy.IsBetterThanCurrent())
				DesktopProxy.Make();
		}
	}
}