#if UNITY_EDITOR
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.Panels;

namespace api.nox.relay.editor {
	public class RelayEditor : EditorModInitializer {
		internal static EditorModCoreAPI CoreAPI;

		private static EditorPanel         _listPanel;
		private        ListConnectionPanel _list;

		public void OnInitializeEditor(EditorModCoreAPI api) {
			CoreAPI    = api;
			_list      = new ListConnectionPanel();
			_listPanel = api.PanelAPI.AddLocalPanel(_list);
		}

		public void OnUpdateEditor() {
			_list.OnUpdate();
		}

		public void OnDisposeEditor() {
			CoreAPI.PanelAPI.RemoveLocalPanel(_listPanel);
			_listPanel = null;
			_list      = null;
			CoreAPI    = null;
		}
	}
}
#endif