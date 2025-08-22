using Nox.Avatars;
using Nox.Entities;

namespace Nox.Players {
	public abstract class PlayerPhysical : Physical {
		public virtual IRuntimeAvatar GetAvatar()
			=> null;
	}
}