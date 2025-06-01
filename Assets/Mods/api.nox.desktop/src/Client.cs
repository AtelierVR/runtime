using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.Controllers;

namespace api.nox.desktop {
	public class Client : ClientModInitializer {
		internal static ClientModCoreAPI CoreAPI;

		internal static IControllerAPI ControllerAPI
			=> CoreAPI.ModAPI.GetMod("controller").GetClients().FirstOrDefault() as IControllerAPI;

		public void OnInitializeClient(ClientModCoreAPI api) {
			CoreAPI = api;
			Keybindings.Rebind();
			DesktopController.Make();
		}

		public void OnDisposeClient() {
			CoreAPI = null;
			Keybindings.Clear();
			if (ControllerAPI?.GetCurrent() is DesktopController)
				ControllerAPI.SetCurrent(null);
		}
	}
}