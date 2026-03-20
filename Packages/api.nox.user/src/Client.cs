using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.user.client;
using api.nox.user.widget;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Network;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.Widgets;
using UnityEngine;

namespace api.nox.user {
	public class Client : IClientModInitializer {
		internal static IUiAPI UiAPI
			=> Main.Instance.CoreAPI.ModAPI
				.GetMod("ui")
				.GetInstance<IUiAPI>();

		public static T GetAsset<T>(ResourceIdentifier path) where T : UnityEngine.Object
			=> Instance.CoreAPI.AssetAPI.GetAsset<T>(path);

		public static UniTask<T> GetAssetAsync<T>(ResourceIdentifier path) where T : UnityEngine.Object
			=> Main.Instance.CoreAPI.AssetAPI.GetAssetAsync<T>(path);

		private EventSubscription[] _events = Array.Empty<EventSubscription>();

		internal static Client           Instance;
		internal        IClientModCoreAPI CoreAPI;

		public void OnInitializeClient(IClientModCoreAPI api) {
			Instance = this;
			CoreAPI  = api;
			_events = new[] {
				CoreAPI.EventAPI.Subscribe("menu_goto", OnGoto),
				CoreAPI.EventAPI.Subscribe("widget_request", OnWidgetRequest)
			};
		}

		private void OnGoto(EventData context) {
			if (!context.TryGet(0, out int mid)) return;
			if (!context.TryGet(1, out string key)) return;
			var menu = UiAPI?.Get<IMenu>(mid);
			if (menu == null) return;
			IPage page = null;
			if (UserPage.GetStaticKey() == key)
				page = UserPage.OnGotoAction(menu, context.Data[2..]);
			if (AuthServerPage.GetStaticKey() == key)
				page = AuthServerPage.OnGotoAction(menu, context.Data[2..]);
			if (page == null) return;
			Main.Instance.CoreAPI.EventAPI.Emit("menu_display", menu.Id, page);
		}

		private void OnWidgetRequest(EventData context) {
			if (!context.TryGet(0, out int mid)) return;
			if (!context.TryGet(1, out RectTransform tr)) return;
			var menu = UiAPI?.Get<IMenu>(mid);
			if (menu == null) return;
			List<(GameObject, IWidget)> widgets = new();
			if (UserWidget.TryMake(menu, tr, out var widget))
				widgets.Add(widget);
			if (AuthWidget.TryMake(menu, tr, out var authWidget))
				widgets.Add(authWidget);
			foreach (var value in widgets)
				context.Callback(value.Item2, value.Item1);
		}

		public void OnDisposeClient() {
			foreach (var e in _events)
				CoreAPI.EventAPI.Unsubscribe(e);
			_events  = Array.Empty<EventSubscription>();
			CoreAPI  = null;
			Instance = null;
		}
	}
}