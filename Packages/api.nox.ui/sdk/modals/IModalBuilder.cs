using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nox.UI.modals {
	public interface IModalBuilder {
		/// <summary>
		/// Sets the title of the modal.
		/// The title is overridden if SetContent is used with a generator function that creates its own title.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="args"></param>
		public void SetTitle(string text, params string[] args);

		/// <summary>
		/// Sets whether the modal can be closed by the user.
		/// </summary>
		/// <param name="closable"></param>
		public void SetClosable(bool closable);
		
		/// <summary>
		/// Returns whether the modal can be closed by the user.
		/// </summary>
		/// <returns></returns>
		public bool IsClosable();

		/// <summary>
		/// Sets the content of the modal to a simple text string.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="args"></param>
		public void SetContent(string text, params string[] args);

		/// <summary>
		/// Sets the content of the modal using a generator function.
		/// </summary>
		/// <param name="generator">
		/// The generator function that takes a RectTransform (the content area),
		/// an IMenu (the menu that created the modal),
		/// and returns a GameObject (the content).
		/// </param>
		public void SetContent(Func<RectTransform, GameObject> generator);

		/// <summary>
		/// Sets options for the modal.
		/// </summary>
		/// <param name="onValue"></param>
		/// <param name="options"></param>
		public void SetOptions(Action<string> onValue, Dictionary<string, string[]> options);

		/// <summary>
		/// Builds the modal and returns the root GameObject.
		/// </summary>
		/// <returns></returns>
		public IModal Build();
	}
}