using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nox.UI {
	/// <summary>
	/// Interface for a page in a menu.
	/// </summary>
	public interface IPage {
		/// <summary>
		/// Gets the unique key of the page.
		/// </summary>
		/// <returns></returns>
		public string GetKey();

		/// <summary>
		/// Gets the context of the page.
		/// </summary>
		/// <returns></returns>
		public object[] GetContext();

		/// <summary>
		/// Make or return <see cref="GameObject"/> for the content of the page.
		/// </summary>
		/// <returns></returns>
		public GameObject GetContent(RectTransform parent)
			=> null;

		/// <summary>
		/// Asynchronously make or return <see cref="GameObject"/> for the content of the page.
		/// </summary>
		/// <param name="parent"></param>
		/// <returns></returns>
		public UniTask<GameObject> GetContentAsync(RectTransform parent)
			=> UniTask.FromResult(GetContent(parent));

		/// <summary>
		/// Gets the menu associated with the page.
		/// </summary>
		/// <returns></returns>
		public IMenu GetMenu();

		/// <summary>
		/// Called when the page is opened.
		/// Is called one time after the page is created and before the first <see cref="OnDisplay(IPage)"/> call.
		/// </summary>
		/// <param name="lastPage"></param>
		public void OnOpen(IPage lastPage) { }

		/// <summary>
		/// Called when the user go back to the page.
		/// </summary>
		/// <param name="lastPage"></param>
		public void OnRestore(IPage lastPage) { }

		/// <summary>
		/// Called when a refresh is requested.
		/// </summary>
		public void OnRefresh() { }

		/// <summary>
		/// Called when the page is removed form history.
		/// </summary>
		public void OnRemove() { }

		/// <summary>
		/// Called when the page is displayed.
		/// Allways called and after <see cref="OnOpen(IPage)"/> or <see cref="OnRestore(IPage)"/>.
		/// </summary>
		/// <param name="lastPage"></param>
		public void OnDisplay(IPage lastPage) { }

		/// <summary>
		/// Called when the page is hidden.
		/// </summary>
		public void OnHide(IPage nextPage) { }
	}
}