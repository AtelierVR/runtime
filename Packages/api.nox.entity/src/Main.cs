using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Entities;

namespace api.nox.entity {
	public class Main : IEntityAPI, MainModInitializer {
		
		[NoxPublic(NoxAccess.Method)]
		public IEntityManager New()
			=> new EntityManager();
	}
}