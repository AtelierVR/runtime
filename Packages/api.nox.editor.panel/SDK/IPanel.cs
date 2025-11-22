using System.Collections.Generic;
using Nox.CCK.Utils;

namespace Nox.Editor.Panel {
	public interface IPanel {
		/// <summary>
		/// Get the unique identifier of this panel.
		/// </summary>
		/// <returns></returns>
		public string[] GetPath();

		/// <summary>
		/// Check if multiple instances of this panel are allowed.
		/// </summary>
		/// <returns></returns>
		public bool AllowMultiple()
			=> false;

		/// <summary>
		/// Get an instance of the panel.
		/// </summary>
		/// <returns></returns>
		public IInstance[] GetInstances();

		/// <summary>
		/// Gets the name of the panel.
		/// </summary>
		/// <returns>The panel name.</returns>
		public string GetLabel();

		/// <summary>
		/// Creates the main visual element for the panel.
		/// </summary>
		/// <param name="window"></param>
		/// <param name="data">A dictionary of data to use when building the panel.</param>
		/// <returns>The panel's visual element.</returns>
		public IInstance Instantiate(IWindow window, Dictionary<string, object> data);
	}
}