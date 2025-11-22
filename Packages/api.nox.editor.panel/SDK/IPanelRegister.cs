using Nox.CCK.Mods.Initializers;

namespace Nox.Editor.Panel {
	public interface IPanelRegister : IEditorModInitializer {
		/// <summary>
		/// Get all registered panels.
		/// </summary>
		/// <returns></returns>
		public IPanel[] GetPanels();

		/// <summary>
		/// Check if a panel is registered.
		/// And if it is, output the panel.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="panel"></param>
		/// <returns></returns>
		public bool TryGetPanel(string[] path, out IPanel panel);
	}
}