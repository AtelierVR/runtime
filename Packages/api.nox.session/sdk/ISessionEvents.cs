using Nox.Players;
using UnityEngine.Events;

namespace Nox.Sessions {
	public interface ISessionEvents {
		public void AddPlayerJoinedListener(UnityAction<IPlayer> action);

		public void AddPlayerLeftListener(UnityAction<IPlayer> action);

		public void AddAuthorityTransferredListener(UnityAction<IPlayer> action);

		public void AddStateChangedListener(UnityAction<IAdapterState, IAdapterState> action);

		public void RemovePlayerJoinedListener(UnityAction<IPlayer> action);

		public void RemovePlayerLeftListener(UnityAction<IPlayer> action);

		public void RemoveAuthorityTransferredListener(UnityAction<IPlayer> action);

		public void RemoveStateChangedListener(UnityAction<IAdapterState, IAdapterState> action);
	}
}