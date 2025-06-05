namespace api.nox.ui.layouts {
	public class BottomOrbiter : Orbiter {
		public Favorites    favorites;
		public Applications applications;
		public Specials     specials;

		public override Part[] GetInternalParts()
			=> new Part[] { favorites, applications, specials };
	}
}