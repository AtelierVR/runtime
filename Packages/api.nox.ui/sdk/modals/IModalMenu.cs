using UnityEngine;

namespace Nox.UI.modals {
	public interface IModalMenu : IMenu {
		public RectTransform GetModalContainer();

		public bool GetActiveForeground();

		public void SetActiveForeground(bool active);

		public IModal[] GetModals();

		public void RegisterModal(IModal modal);

		public void UnregisterModal(IModal modal);
	}
}