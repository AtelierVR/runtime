using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.Controllers;
using Nox.UI;

namespace api.nox.desktop {
	public class Client : ClientModInitializer {
		internal static ClientModCoreAPI CoreAPI;

		internal static IControllerAPI ControllerAPI
			=> CoreAPI.ModAPI.GetMod("controller").GetMains().FirstOrDefault() as IControllerAPI;

		internal static IUiAPI UiAPI
			=> CoreAPI.ModAPI.GetMod("ui").GetClients().FirstOrDefault() as IUiAPI;

		public void OnInitializeClient(ClientModCoreAPI api) {
			CoreAPI = api;
			Keybindings.Rebind();
			DesktopController.Make();
		}

		public void OnDisposeClient() {
			if (ControllerAPI.GetCurrent() is DesktopController)
				ControllerAPI.SetCurrent(null);
			Keybindings.Clear();
			CoreAPI = null;
		}
	}
}