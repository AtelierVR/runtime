using System.IO;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using UnityEngine.Events;

namespace api.nox.jint {
	public class Main : MainModInitializer {
		public        Manager     Manager;
		public static Main        Instance;
		public        IModCoreAPI CoreAPI;

		public static readonly UnityEvent<JintBacking> OnBackingAdded   = new();
		public static readonly UnityEvent<JintBacking> OnBackingRemoved = new();

		public static string GetModulePath() {
			var folder = Path.Combine(Constants.ConfigPath, "jint_modules");
			if (!Directory.Exists(folder))
				Directory.CreateDirectory(folder);
			return folder;
		}

		public void OnInitializeMain(MainModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;
			Manager  = new Manager();
		}
	}
}