using UnityEngine;
using UnityEngine.Events;

namespace Nox.UI.modals {
	public interface IModal {
		public void Close();

		public void Show();

		public bool IsOpen();

		public IModalMenu GetMenu();

		public GameObject GetContent();

		public void Dispose();

		public UnityEvent OnClose { get; }

		public UnityEvent OnOpen { get; }
	}
}