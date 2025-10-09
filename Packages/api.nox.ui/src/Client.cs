using api.nox.ui.modals;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;

namespace api.nox.ui {
	public class Client : IUiAPI, IClientModInitializer {
		internal        MenuManager      Manager;
		private         PageManager      _pages;
		public          ClientModCoreAPI CoreAPI;
		internal static Client           Instance;

		public void OnInitializeClient(ClientModCoreAPI api) {
			CoreAPI  = api;
			Instance = this;
			Manager  = new MenuManager(this);
			_pages   = new PageManager(this);
		}

		public void OnDisposeClient() {
			Manager.Dispose();
			_pages.Dispose();
			Manager = null;
			Manager = null;
		}

		public T Get<T>(int id) where T : IMenu
			=> Manager.Get<T>(id);

		public bool Has(int id)
			=> Manager.Has(id);

		public void Add(IMenu menu)
			=> Manager.Add(menu);

		public void Remove(int id)
			=> Manager.Remove(id);

		public IMenu Make(RectTransform container, GameObject parent = null)
			=> Manager.Make(container, parent);

		public void SendGoto(int menuId, string key, params object[] args)
			=> PageManager.SendGoto(menuId, key, args);

		public void SendAction(int menuId, string action, int move = 0)
			=> PageManager.SendAction(menuId, action, move);

		public void SendDisplay(int menuId, IPage page)
			=> PageManager.SendDisplay(menuId, page);

		public IModalBuilder MakeModal(IMenu menu) {
			if (menu is IModalMenu m)
				return new ModalBuilder(m);
			Debug.LogError($"Menu {menu} is not a modal menu");
			return null;
		}
	}
}