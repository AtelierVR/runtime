using UnityEngine;

namespace Nox.UI.modals {
	public interface IModal {
		public void Close();

		public void Show();

		public bool IsOpen();

		public IModalMenu GetMenu();

		public GameObject GetContent();

		public void Dispose();
	}
}