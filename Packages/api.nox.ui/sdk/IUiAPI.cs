using Cysharp.Threading.Tasks;
using Nox.UI.modals;
using UnityEngine;

namespace Nox.UI {
	/// <summary>
	/// Interface for the UI API, providing methods to manage menus.
	/// </summary>
	public interface IUiAPI {
		/// <summary>
		/// Gets a menu by its ID.
		/// </summary>
		/// <param name="id"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T Get<T>(int id) where T : IMenu;

		/// <summary>
		/// Checks if a menu exists in the manager.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool Has(int id);

		/// <summary>
		/// Adds a menu to the manager.
		/// </summary>
		/// <param name="menu"></param>
		public void Add(IMenu menu);

		/// <summary>
		/// Removes a menu from the manager by its ID.
		/// </summary>
		/// <param name="id"></param>
		public void Remove(int id);

		/// <summary>
		/// Create a main default menu.
		/// </summary>
		/// <param name="container"></param>
		/// <returns></returns>
		public UniTask<IMenu> Make(IMenuProvider container);

		/// <summary>
		/// Sends a goto event to the menu with the specified ID and key.
		/// </summary>
		/// <param name="menuId"></param>
		/// <param name="key"></param>
		/// <param name="args"></param>
		public void SendGoto(int menuId, string key, params object[] args);

		/// <summary>
		/// Sends an action event to the menu with the specified ID.
		/// </summary>
		/// <param name="menuId"></param>
		/// <param name="action"></param>
		/// <param name="move"></param>
		public void SendAction(int menuId, string action, int move = 0);

		/// <summary>
		/// Sends a display event to the menu with the specified ID and page.
		/// </summary>
		/// <param name="menuId"></param>
		/// <param name="page"></param>
		public void SendDisplay(int menuId, IPage page);

		/// <summary>
		/// Creates a new modal builder instance.
		/// </summary>
		/// <param name="menu"></param>
		/// <returns></returns>
		public IModalBuilder MakeModal(IMenu menu);
	}
}