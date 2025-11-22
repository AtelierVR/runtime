using System.Collections.Generic;
using Nox.CCK.Utils;

namespace Nox.Editor.Panel {
	public interface IPanelAPI {
		/// <summary>
		/// Get all available panels.
		/// </summary>
		/// <returns></returns>
		public IPanel[] GetPanels();

		/// <summary>
		/// Check if a panel exists by its ID.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="panel"></param>
		/// <returns></returns>
		public bool TryGetPanel(ResourceIdentifier id, out IPanel panel);

		/// <summary>
		/// Try to Open a panel in a new window.
		/// </summary>
		/// <param name="panel"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public bool TryOpen(IPanel panel, Dictionary<string, object> data = null);
	}
}