using Nox.Players;
using Nox.Worlds;

namespace Nox.Sessions {
	public interface ISessionModule : IWorldModule {
		public void OnSession(ISession session) { }

		public void OnSessionSelected() { }

		public void OnSessionDeselected() { }

		public void OnPlayerJoined(IPlayer player) { }

		public void OnPlayerLeft(IPlayer player) { }

		public void OnAuthorityTransferred(IPlayer @new) { }
	}
}