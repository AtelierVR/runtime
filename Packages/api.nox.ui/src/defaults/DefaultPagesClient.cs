using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.UI;

namespace api.nox.ui.defaults {
	public class DefaultPagesClient : IClientModInitializer {
		private IClientModCoreAPI  _coreAPI;
		private EventSubscription _event;

		public void OnInitializeClient(IClientModCoreAPI api) {
			_coreAPI = api;
			_event   = _coreAPI.EventAPI.Subscribe(PageManager.GotoEvent, OnGoto);
		}

		private void OnGoto(EventData context) {
			if (!context.TryGet(0, out int mid)) return;
			if (!context.TryGet(1, out string key)) return;
			var menu = Client.Instance?.Get<IMenu>(mid);
			if (menu == null) return;
			IPage page = null;
			if (HomePage.GetStaticKey() == key)
				page = HomePage.OnGotoAction(menu, context.Data[2..]);
			else if (ExamplePage.GetStaticKey() == key)
				page = ExamplePage.OnGotoAction(menu, context.Data[2..]);
			if (page == null) return;
			_coreAPI.EventAPI.Emit(PageManager.DisplayEvent, menu.GetId(), page);
		}

		public void OnDisposeClient() {
			_coreAPI.EventAPI.Unsubscribe(_event);
			_event   = null;
			_coreAPI = null;
		}
	}
}