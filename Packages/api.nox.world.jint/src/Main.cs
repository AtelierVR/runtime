using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using UnityEngine.Events;

namespace api.nox.world.jint {
	public class Main : MainModInitializer {
		internal static MainModCoreAPI CoreAPI;
		internal static Main           Instance;
		internal        WorldManager   WorldManager;

		internal static MainModInitializer WorldAPI
			=> CoreAPI.ModAPI.GetMod("world").GetMains().FirstOrDefault();

		internal static MainModInitializer JintAPI
			=> CoreAPI.ModAPI.GetMod("jint").GetMains().FirstOrDefault();

		public void OnInitializeMain(MainModCoreAPI api) {
			CoreAPI      = api;
			Instance     = this;
			WorldManager = new WorldManager();
			Logger.LogDebug($"WorldManager: {WorldManager}");
		}

		public void OnDisposeMain() {
			WorldManager.Dispose();
			WorldManager = null;
			CoreAPI  = null;
			Instance = null;
		}
	}
}