using System.Linq;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;
using UnityEngine.Events;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.ui.modals {
	public sealed class BaseModal : MonoBehaviour, IModal {
		public GameObject content;
		public IModalMenu Menu;

		public void Close() {
			InternalClose();
			OnClose.Invoke();
		}

		private void InternalClose() {
			gameObject.SetActive(false);
			if (Menu == null) return;
			var modals = Menu.GetModals();
			var active = modals.Any(m => m.IsOpen());
			Menu.SetActiveForeground(active);
		}

		public void Show() {
			gameObject.SetActive(true);
			transform.SetAsLastSibling();
			Menu.SetActiveForeground(true);
			OnOpen.Invoke();
		}

		public bool IsOpen()
			=> gameObject.activeSelf;

		public IModalMenu GetMenu()
			=> Menu;

		public GameObject GetContent()
			=> content;

		private void OnDestroy()
			=> Dispose();

		public void Dispose() {
			InternalClose();
			Menu?.UnregisterModal(this);
			Menu = null;
		}

		public UnityEvent OnClose { get; } = new();
		public UnityEvent OnOpen  { get; } = new();

		public void OnCloseClicked()
			=> Dispose();

		public void Attach(IModalMenu menu) {
			if (Menu == menu) return;
			Menu?.UnregisterModal(this);
			Menu = menu;
			Menu?.RegisterModal(this);
		}
	}
}