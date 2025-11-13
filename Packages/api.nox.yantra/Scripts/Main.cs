using System.IO;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.YantraJS;
using UnityEngine.Events;

namespace api.nox.yantra {
	public class Main : IMainModInitializer, IYantraAPI {
		public        Manager     Manager;
		public static Main        Instance;
		public        IModCoreAPI CoreAPI;

		public static readonly UnityEvent<YantraBacking> OnBackingAdded   = new();
		public static readonly UnityEvent<YantraBacking> OnBackingRemoved = new();

		public string GetModulesPath() {
			var folder = Path.Combine(Constants.ConfigPath, "yantra_modules");
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

