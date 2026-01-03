using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Entities;

namespace api.nox.entity {
	public class Main : IEntityAPI, IMainModInitializer {
		
		[NoxPublic(NoxAccess.Method)]
		public IEntities New()
			=> new Entities();
	}
}