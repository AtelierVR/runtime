using Nox.Entities;
using Nox.Players;
using Nox.Worlds;
using UnityEngine;

namespace Nox.Sessions {
	public interface ISessionModule : IWorldModule {
		public void OnLoaded(ISession session) { }

		public void OnSessionSelected() { }

		public void OnSessionDeselected() { }

		public void OnPlayerJoined(IPlayer player) { }

		public void OnPlayerLeft(IPlayer player) { }

		public void OnAuthorityTransferred(IPlayer @new) { }

		public void OnSceneLoaded(IWorldDescriptor descriptor, int index, GameObject anchor) { }

		public void OnSceneUnloaded(int index) { }

		public void OnEntityRegistered(IEntity entity) { }

		public void OnEntityUnregistered(IEntity entity) { }
	}
}