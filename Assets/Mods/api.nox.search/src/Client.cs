using System.Linq;
using api.nox.search.client;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.UI;

namespace api.nox.search {
	public class Client : ClientModInitializer {
		internal static IUiAPI UiAPI
			=> Main.Instance.CoreAPI.ModAPI
				.GetMod("ui")
				.GetClients()
				.FirstOrDefault() as IUiAPI;

		public void OnInitializeClient(ClientModCoreAPI api)
			=> SearchPage.Listen();

		public void OnDisposeClient()
			=> SearchPage.StopListen();
	}
}