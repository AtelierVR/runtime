/*#if UNITY_EDITOR
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.Panels;

namespace api.nox.session {
	public class Editor : IEditorModInitializer {
		private         SessionsPanel    _sessionsPanel;
		private static  IEditorPanel      _sessionsPanelInstance;
		internal static IEditorModCoreAPI CoreAPI;

		public void OnInitializeEditor(IEditorModCoreAPI api) {
			CoreAPI                = api;
			_sessionsPanel         = new SessionsPanel();
			_sessionsPanelInstance = api.PanelAPI.AddLocalPanel(_sessionsPanel);
		}

		public void OnDisposeEditor() {
			CoreAPI.PanelAPI.RemoveLocalPanel(_sessionsPanelInstance);
			_sessionsPanel?.Dispose();
			_sessionsPanel         = null;
			_sessionsPanelInstance = null;
			CoreAPI                = null;
		}

		public void OnUpdateEditor() {
			if (!_sessionsPanelInstance.IsActive()) return;
			_sessionsPanel?.Update();
		}

		internal static bool HasSessionPanelOpened()
			=> _sessionsPanelInstance?.IsActive() ?? false;
	}
}
#endif*/