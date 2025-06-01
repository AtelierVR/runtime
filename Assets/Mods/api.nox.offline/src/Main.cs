using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Entities;
using Nox.Offline;
using Nox.Players;
using Nox.Worlds;

namespace api.nox.offline {
	public class Main : IOfflineAPI, MainModInitializer {
		internal static IEntityAPI EntityAPI
			=> _coreAPI.ModAPI.GetMod("entity").GetMains().FirstOrDefault() as IEntityAPI;

		internal static IPlayerAPI PlayerAPI
			=> _coreAPI.ModAPI.GetMod("player").GetMains().FirstOrDefault() as IPlayerAPI;

		private static MainModCoreAPI _coreAPI;

		public void OnInitializeMain(MainModCoreAPI api)
			=> _coreAPI = api;

		public void OnDisposeMain()
			=> _coreAPI = null;

		[NoxPublic(NoxAccess.Method)]
		public IOfflineAdapter New(IWorld world)
			=> new OfflineAdapter(world);
	}
}