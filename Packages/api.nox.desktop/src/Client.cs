using System.Linq;
using Nox.Avatars;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.Controllers;
using Nox.UI;
using Nox.Users;

namespace api.nox.desktop {
	public class Client : ClientModInitializer {
		internal static ClientModCoreAPI CoreAPI;

		internal static IControllerAPI ControllerAPI
			=> CoreAPI.ModAPI
				.GetMod("controller")
				.GetEntry<IControllerAPI>();

		internal static IUiAPI UiAPI
			=> CoreAPI.ModAPI
				.GetMod("ui")
				.GetEntry<IUiAPI>();

		internal static IAvatarAPI AvatarAPI
			=> CoreAPI.ModAPI
				.GetMod("avatar")
				.GetEntry<IAvatarAPI>();
		
		internal static IUserAPI UserAPI
			=> CoreAPI.ModAPI
				.GetMod("user")
				.GetEntry<IUserAPI>();

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