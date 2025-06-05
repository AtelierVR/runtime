using Nox.UI;

namespace api.nox.ui.layouts {
	public class TopOrbiter : Orbiter {
		public Histories histories;
		public Actions   actions;

		public override Part[] GetInternalParts()
			=> new Part[] { actions, histories };
	}
}