using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.Widgets;

namespace api.nox.widget {
	public class Client : IWidgetAPI, ClientModInitializer {
		public          ClientModCoreAPI CoreAPI;
		private         WidgetManager    _manager;

		public void OnInitializeClient(ClientModCoreAPI api) {
			CoreAPI  = api;
			_manager  = new WidgetManager(this);
		}

		public void OnDisposeClient() {
			_manager.Dispose();
			_manager = null;
		}

		public bool Has(string key)
			=> _manager.Has(key);

		public T Get<T>(string key) where T : IWidget
			=> _manager.Get<T>(key);

		public void Set(IWidget widget)
			=> _manager.Set(widget);

		public void Remove(string key)
			=> _manager.Remove(key);
	}
}