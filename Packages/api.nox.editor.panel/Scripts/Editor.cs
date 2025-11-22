using System.Collections.Generic;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Editor.Panel;

namespace api.nox.editor.panel {
	public class Editor : IEditorModInitializer, IPanelAPI {
		internal static EditorModCoreAPI CoreAPI;

		public void OnInitializeEditor(EditorModCoreAPI api)
			=> CoreAPI = api;

		public void OnPostInitializeEditor() {
			foreach (var window in WindowManager.GetWindows()) 
				window.Repaint();
		}

		public void OnDisposeEditor() {
			foreach (var window in WindowManager.GetWindows())
				window.Close();
				
			CoreAPI = null;
		}

		public IPanel[] GetPanels()
			=> PanelManager.GetPanels();

		public bool TryGetPanel(ResourceIdentifier id, out IPanel panel)
			=> PanelManager.TryGetPanel(id, out panel);

		public bool TryOpen(IPanel panel, Dictionary<string, object> data = null)
			=> WindowManager.TryOpen(panel, data);
	}
}