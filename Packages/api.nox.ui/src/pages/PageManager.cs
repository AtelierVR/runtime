using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.ui.pages;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Utils;
using Nox.UI;

namespace api.nox.ui {
	public class PageManager : INoxObject, IDisposable {
		public const string GotoEvent    = "menu_goto";
		public const string ActionEvent  = "menu_action";
		public const string DisplayEvent = "menu_display";

		private readonly EventSubscription[] _events;
		private readonly Client              _client;

		public PageManager(Client client) {
			_client = client;
			_events = new[] {
				_client.CoreAPI.EventAPI.Subscribe(DisplayEvent, OnDisplay),
				_client.CoreAPI.EventAPI.Subscribe(ActionEvent, OnAction),
			};
		}

		public static async UniTask<T> GetAssetAsync<T>(ResourceIdentifier path) where T : UnityEngine.Object
			=> await GetCoreAPI().AssetAPI.GetAssetAsync<T>(path);

		public static IModCoreAPI GetCoreAPI()
			#if UNITY_EDITOR
			=> Editor.CoreAPI;
		#else
			=> Client.Instance.CoreAPI;
		#endif

		public static void SendAction(int menuId, string action, int move = 0)
			=> GetCoreAPI().EventAPI.Emit(ActionEvent, menuId, action, move);

		public static void SendDisplay(int menuId, IPage page)
			=> GetCoreAPI().EventAPI.Emit(DisplayEvent, menuId, page);

		public static void SendGoto(int menuId, string key, params object[] args)
			=> GetCoreAPI()
				.EventAPI.Emit(
					GotoEvent, new object[] { menuId, key }.Concat(args).ToArray()
				);

		private void OnAction(EventData context) {
			if (!context.TryGet(0, out int id) || !context.TryGet(1, out string action)) return;
			var menu = _client.Manager.Get<IMenu>(id);
			if (menu == null) return;
			switch (action) {
				case "move":
					if (!context.TryGet(0, out int move) || move == 0) return;
					if (move < 0) menu.GoBack(-move);
					else menu.GoForward(move);
					break;
				case "back":
					menu.GoBack();
					break;
				case "forward":
					menu.GoForward();
					break;
				case "refresh":
					menu.GetCurrent()?.OnRefresh();
					break;
			}
		}

		private void OnDisplay(EventData context) {
			if (!context.TryGet(0, out int id)) return;
			var menu = _client.Manager.Get<IMenu>(id);
			context.TryGet(1, out IPage page);
			if (context.TryGet(1, out Dictionary<string, object> data))
				page = ActionPage.From(data);
			if (page == null) return;
			menu.Go(page);
		}

		public void Dispose() {
			foreach (var ev in _events)
				_client.CoreAPI.EventAPI.Unsubscribe(ev);
		}
	}
}