#if UNITY_EDITOR
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.Panels;

namespace api.nox.controller {
	public class Editor : EditorModInitializer {
		internal static EditorModCoreAPI CoreAPI;

		private static EditorPanel     _controllerPanel;
		private        ControllerPanel _controller;

		public void OnInitializeEditor(EditorModCoreAPI api) {
			CoreAPI          = api;
			_controller      = new ControllerPanel();
			_controllerPanel = api.PanelAPI.AddLocalPanel(_controller);
		}

		public void OnUpdateEditor() {
			_controller.OnUpdate();
		}

		public void OnDisposeEditor() {
			CoreAPI.PanelAPI.RemoveLocalPanel(_controllerPanel);
			_controllerPanel = null;
			_controller?.Dispose();
			_controller = null;
			CoreAPI     = null;
		}
	}
}
#endif // UNITY_EDITOR
