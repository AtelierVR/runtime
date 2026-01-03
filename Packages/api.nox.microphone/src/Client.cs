using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;

namespace api.nox.microphone {
	public class Client : IClientModInitializer {
		public void OnInitializeClient(IClientModCoreAPI api)
			=> Main.Instance?.Manager
				.CurrentMicrophone
				?.Start("current");


		public void OnDisposeClient()
			=> Main.Instance?.Manager
				.CurrentMicrophone
				?.Stop("current");
	}
}