using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine.Events;

namespace api.nox.jint {
	public class Main : MainModInitializer {
		public        Manager    Manager;
		public static Main       Instance;
		public        ModCoreAPI CoreAPI;

		public static readonly UnityEvent<JintBacking> OnBackingAdded = new();
		public static readonly UnityEvent<JintBacking> OnBackingRemoved = new();

		public void OnInitializeMain(MainModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;
			Manager  = new Manager();
		}
	}
}