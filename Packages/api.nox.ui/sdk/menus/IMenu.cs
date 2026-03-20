using Nox.UI.modals;

namespace Nox.UI {
	/// <summary>
	/// Interface for a menu in the UI.
	/// </summary>
	public interface IMenu {
		/// <summary>
		/// Get the unique identifier of the menu.
		/// </summary>
		/// <returns></returns>
		public int Id { get; }
		
		/// <summary>
		/// Get if the menu is displayed or not.
		/// </summary>
		/// <returns></returns>
		public bool Active { get; set;  }
		
		/// <summary>
		/// Get all orbiters in the menu.
		/// </summary>
		/// <returns></returns>
		public IOrbiter[] GetOrbiters();

		/// <summary>
		/// Get part by its key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public IPart GetPart(string key);

		/// <summary>
		/// Close the menu and remove it from the UI.
		/// </summary>
		public void Dispose();

		/// <summary>
		/// Add to history and go to the page.
		/// </summary>
		/// <param name="page"></param>
		public void Go(IPage page);

		/// <summary>
		/// Go back in history.
		/// If there is no back history, it will do nothing.
		/// Or if the count is greater than the back history, it will go to the first page in history.
		/// </summary>
		/// <param name="count">Number of pages to go back, default is 1.</param>
		public void GoBack(int count = 1);

		/// <summary>
		/// Go forward in history.
		/// If there is no forward history, it will do nothing.
		/// Or if the count is greater than the forward history, it will go to the last page in history.
		/// </summary>
		/// <param name="count">Number of pages to go forward, default is 1.</param>
		public void GoForward(int count = 1);

		/// <summary>
		/// Get the current page in history.
		/// </summary>
		/// <returns></returns>
		public IPage GetCurrent();
	}
}