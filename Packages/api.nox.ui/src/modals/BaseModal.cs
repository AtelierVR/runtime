using System.Linq;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;

namespace api.nox.ui.modals {
	public sealed class BaseModal : MonoBehaviour, IModal {
		public GameObject content;
		public IModalMenu Menu;

		public void Close() {
			gameObject.SetActive(false);
			var modals = Menu.GetModals();
			var active = modals.Any(m => m.IsOpen());
			Menu.SetActiveForeground(active);
		}

		public void Show() {
			gameObject.SetActive(true);
			transform.SetAsLastSibling();
			Menu.SetActiveForeground(true);
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
			Close();
			if (gameObject)
				Destroy(gameObject);
			Menu?.UnregisterModal(this);
			Menu = null;
		}

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