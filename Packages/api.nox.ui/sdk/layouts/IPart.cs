using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nox.UI {
	/// <summary>
	/// IPart is a list of elements that placed in container.
	/// </summary>
	public interface IPart {
		/// <summary>
		/// Get the key (id) of the part.
		/// </summary>
		/// <returns></returns>
		public string GetKey();

		/// <summary>
		/// Get if the part is displayed or not.
		/// </summary>
		/// <returns></returns>
		public bool GetActive();

		/// <summary>
		/// Set if the part is displayed or not.
		/// </summary>
		/// <param name="active"></param>
		public void SetActive(bool active);

		/// <summary>
		/// Get all elements in the part.
		/// </summary>
		/// <returns></returns>
		public NavigationData[] GetElements();

		/// <summary>
		/// Remove an element from the part.
		/// </summary>
		public void RemoveElement(string key);

		/// <summary>
		/// Add an element to the part.
		/// If is already exists, update it.
		/// </summary>
		/// <param name="element"></param>
		public UniTask AddElement(NavigationData element);

		/// <summary>
		/// Add multiple elements to the part.
		/// If already exists, update it.
		/// </summary>
		/// <param name="elements"></param>
		UniTask AddElements(NavigationData[] elements);
	}
}