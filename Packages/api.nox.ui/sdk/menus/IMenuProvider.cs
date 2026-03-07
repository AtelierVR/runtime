using UnityEngine;
namespace Nox.UI {
	public interface IMenuProvider {
		/// <summary>
		/// The container of the menu,
		/// which is a RectTransform in the UI
		/// who is instantiated
		/// when the menu is created.
		/// </summary>
		public RectTransform Container { get; }
		
		/// <summary>
		/// Action to call when the menu is closed,
		/// to disable the menu.
		/// </summary>
		public bool Active { get; set; }
	}
}