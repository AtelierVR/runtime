using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.ui.menus;
using api.nox.ui.pages;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.ui {
	public class UIClient : ClientModInitializer {
		public void OnInitializeClient(ClientModCoreAPI api) {
			Instance = this;
			Cache    = new List<Menu>();
			_customs = new CustomPageHelper[] {
				new HomePage(),
				new HelloPage(),
			};
		}

		internal static UIClient           Instance;
		internal        List<Menu>         Cache;
		private         CustomPageHelper[] _customs;

		[NoxPublic(NoxAccess.Method)]
		public PhysicalMenu SpawnPhysical(Vector3 position, Quaternion rotation) {
			var menu = PhysicalMenu.Create(position, rotation);
			Add(menu);
			return menu;
		}

		[NoxPublic(NoxAccess.Method)]
		public ViewportMenu GetViewport()
			=> Cache.FirstOrDefault(m => m is ViewportMenu) as ViewportMenu;

		[NoxPublic(NoxAccess.Method)]
		public ViewportMenu SpawnViewport(RectTransform parent) {
			var menu = ViewportMenu.Create(parent);
			Add(menu);
			return menu;
		}

		private bool Has(Menu menu)
			=> Cache.Exists(m => m.GetId() == menu.GetId());

		internal T Get<T>(int menuId) where T : Menu
			=> Cache.Find(m => m.GetId() == menuId && m is T) as T;

		private void Add(Menu menu) {
			if (Has(menu)) return;
			Cache.Add(menu);
			UISystem.CoreAPI.EventAPI.Emit("menu_added", menu);
		}

		private void Remove(int menuId) {
			var menu = Get<Menu>(menuId);
			if (!menu) return;

			var canRemove = true;
			UISystem.CoreAPI.EventAPI.Emit("menu_request_remove", menu, new Action<object[]>(OnMenuRequestRemove));
			if (!canRemove) {
				Logger.LogDebug($"Canceling removing menu {menu.GetId()}");
				return;
			}

			Cache.Remove(menu);
			menu.Dispose();
			UISystem.CoreAPI.EventAPI.Emit("menu_removed", menu);
			return;

			void OnMenuRequestRemove(object[] rms) {
				if (rms.Length > 0 && rms[0] is false)
					canRemove = false;
			}
		}

		public void OnDisposeClient() {
			foreach (var menu in Cache.ToArray())
				Remove(menu.GetId());
			foreach (var custom in _customs)
				custom.Dispose();
			_customs = null;
			Cache    = null;
			Instance = null;
		}
	}
}