using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Nox.CCK.Mods.Panels {
	/// <summary>
	/// Interface for building custom editor panels in the Nox CCK.
	/// </summary>
	public interface IEditorPanelBuilder {
		/// <summary>
		/// Gets the unique identifier of the panel.
		/// </summary>
		/// <returns>The panel ID.</returns>
		public string GetId();
		
		/// <summary>
		/// Gets the name of the panel.
		/// </summary>
		/// <returns>The panel name.</returns>
		public string GetName();

		/// <summary>
		/// Gets the title of the panel (optional).
		/// </summary>
		/// <returns>The panel title, or null if not specified.</returns>
		public string GetTitle()
			=> null;

		/// <summary>
		/// Gets the header visual elements for the panel (optional).
		/// </summary>
		/// <returns>An array of visual elements to display in the header, or an empty array.</returns>
		public VisualElement[] GetHeaders()
			=> Array.Empty<VisualElement>();

		/// <summary>
		/// Determines if the panel should be hidden.
		/// </summary>
		/// <returns>True if the panel should be hidden; otherwise, false.</returns>
		public bool IsHidden();

		/// <summary>
		/// Creates the main visual element for the panel.
		/// </summary>
		/// <param name="data">A dictionary of data to use when building the panel.</param>
		/// <returns>The panel's visual element.</returns>
		public VisualElement Make(Dictionary<string, object> data);

		/// <summary>
		/// Called every frame to update the panel (optional).
		/// </summary>
		public void OnUpdate() { }

		/// <summary>
		/// Called when the panel becomes hidden (optional).
		/// </summary>
		public void OnHidden() { }

		/// <summary>
		/// Called when the panel becomes visible (optional).
		/// </summary>
		public void OnVisible() { }
	}
}