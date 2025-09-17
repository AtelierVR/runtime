using Nox.Players;
using UnityEngine.Events;

namespace Nox.Sessions {
	public interface ISessionEvents {
		public UnityEvent<IPlayer> OnPlayerJoinedEvent();
		public UnityEvent<IPlayer> OnPlayerLeftEvent();
		public UnityEvent<IPlayer> OnAuthorityTransferredEvent();
	}
}