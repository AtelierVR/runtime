// using System.Linq;
// using Nox.CCK.Mods.Cores;
// using Nox.CCK.Mods.Initializers;
// using Nox.UI;
//
// namespace api.nox.user {
// 	public class Client : ClientModInitializer {
// 		internal static IUiAPI UiAPI
// 			=> Main.Instance.CoreAPI.ModAPI
// 				.GetMod("ui")
// 				.GetClients()
// 				.FirstOrDefault() as IUiAPI;
//
//
// 		public void OnInitializeClient(ClientModCoreAPI api) {
// 			// ProfilePage.Listen();
// 		}
//
// 		public void OnDisposeClient() {
// 			// ProfilePage.StopListen();
// 		}
// 	}
// }