using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nox.Editor.Panel {
	public interface IInstance {
		/// <summary>
		/// Get the panel associated with this instance.
		/// </summary>
		/// <returns></returns>
		public IPanel GetPanel();

		/// <summary>
		/// Get the window that this instance belongs to.
		/// </summary>
		/// <returns></returns>
		public IWindow GetWindow();

		/// <summary>
		/// Get the title of this instance.
		/// </summary>
		/// <returns></returns>
		public string GetTitle();

		/// <summary>
		/// Called every frame while this instance is active.
		/// </summary>
		public void OnUpdate() { }

		/// <summary>
		/// Called when the instance is destroyed.
		/// </summary>
		public void OnDestroy() { }

		/// <summary>
		/// Called when the instance is created or re-focused.
		/// </summary>
		public void OnFocus() { }

		/// <summary>
		/// Get the root content VisualElement for this instance.
		/// </summary>
		/// <returns></returns>
		public VisualElement GetContent();
	}
}