using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.ui.layouts;
using api.nox.ui.menus;
using Nox.CCK.Mods.Cores;
using Nox.UI;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Object = UnityEngine.Object;

namespace api.nox.ui {
	public class MenuManager {
		private readonly List<IMenu> _menus = new();
		private readonly Client      _client;

		public MenuManager(Client client)
			=> _client = client;

		public bool Has(int id)
			=> _menus.Any(m => m.GetId() == id);

		public T Get<T>(int id) where T : IMenu
			=> (T)_menus.Find(m => m.GetId() == id && m is T);

		public void Add(IMenu menu) {
			if (Has(menu.GetId())) return;
			_menus.Add(menu);
			_client.CoreAPI.EventAPI.Emit("menu_added", menu);
		}

		public void Remove(int id) {
			var menu = Get<IMenu>(id);
			if (menu == null) return;

			var canRemove = true;
			_client.CoreAPI.EventAPI.Emit("menu_request_remove", menu, new Action<object[]>(OnMenuRequestRemove));
			if (!canRemove) {
				Logger.LogDebug($"Canceling removing menu {menu.GetId()}");
				return;
			}

			_menus.Remove(menu);
			menu.Dispose();
			_client.CoreAPI.EventAPI.Emit("menu_removed", menu);
			return;

			void OnMenuRequestRemove(object[] rms) {
				if (rms.Length > 0 && rms[0] is false)
					canRemove = false;
			}
		}

		public void Dispose() {
			foreach (var menu in _menus)
				menu.Dispose();
			_menus.Clear();
		}

		public Menu Make(RectTransform container, GameObject parent = null) {
			var prefab = PageManager.GetAsset<GameObject>("prefabs/menu.prefab");

			Logger.LogDebug($"Instantiating menu {prefab?.name ?? "null"} into {container?.name ?? "null"}");

			var instance = Object.Instantiate(prefab, container);
			var menu     = instance?.GetComponent<Menu>();
			if (menu == null) {
				Logger.LogError("Failed to get menu component from prefab");
				Object.Destroy(instance);
				return null;
			}

			menu.Client          = _client;
			menu.gameObject.name = $"[{menu.GetType().Name}_{menu.GetInstanceID()}]";
			menu.parent          = parent ?? menu.gameObject;
			Add(menu);
			return menu;
		}
	}
}