#if UNITY_EDITOR
using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.Panels;

namespace api.nox.world {
	public class Editor : EditorModInitializer {
		private        WorldLoaderPanel  _loader;
		private        WorldManagerPanel _manager;
		private static EditorPanel       _loaderPanel;
		private static EditorPanel       _managerPanel;
		private static EditorPanel       _builderPanel;
		private static EditorPanel       _publisherPanel;

		private         WorldPublisherPanel _publisher;
		internal static WorldBuilderPanel   Builder;
		internal static EditorModCoreAPI    CoreAPI;

		internal static MainModInitializer NetworkAPI
			=> CoreAPI.ModAPI
				.GetMod("network")
				.GetMains()
				.FirstOrDefault();

		public void OnInitializeEditor(EditorModCoreAPI api) {
			CoreAPI    = api;
			_loader    = new WorldLoaderPanel();
			Builder    = new WorldBuilderPanel();
			_publisher = new WorldPublisherPanel();
			_manager   = new WorldManagerPanel();

			_loaderPanel    = api.PanelAPI.AddLocalPanel(_loader);
			_builderPanel   = api.PanelAPI.AddLocalPanel(Builder);
			_publisherPanel = api.PanelAPI.AddLocalPanel(_publisher);
			_managerPanel   = api.PanelAPI.AddLocalPanel(_manager);
		}

		public void OnDisposeEditor() {
			Builder.Dispose();
			_manager.Dispose();
			CoreAPI.PanelAPI.RemoveLocalPanel(_loaderPanel);
			CoreAPI.PanelAPI.RemoveLocalPanel(_builderPanel);
			CoreAPI.PanelAPI.RemoveLocalPanel(_publisherPanel);
			CoreAPI.PanelAPI.RemoveLocalPanel(_managerPanel);
			_loader         = null;
			Builder         = null;
			_publisher      = null;
			_loaderPanel    = null;
			_builderPanel   = null;
			_publisherPanel = null;
			_managerPanel   = null;
			CoreAPI         = null;
		}

		public void OnUpdateEditor() {
			Builder.Update();
			_publisher.Update();
		}

		internal static bool HasOnePanelOpened()
			=> (_loaderPanel        != null && _loaderPanel.IsActive())
				|| (_builderPanel   != null && _builderPanel.IsActive())
				|| (_publisherPanel != null && _publisherPanel.IsActive());
	}
}
#endif