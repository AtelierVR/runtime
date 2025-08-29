using System.Linq;
using Cysharp.Threading.Tasks;
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

		public async UniTask OnInitializeClientAsync(ClientModCoreAPI api) {
			CoreAPI = api;
			Keybindings.Rebind();
			await DesktopController.Make();
		}

		public async UniTask OnDisposeClientAsync() {
			if (ControllerAPI.GetCurrent() is DesktopController)
				await ControllerAPI.SetCurrent(null);
			Keybindings.Clear();
			CoreAPI = null;
		}
	}
}