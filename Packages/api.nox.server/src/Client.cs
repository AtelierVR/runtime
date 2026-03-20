using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.server.client;
using api.nox.server.widget;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Network;
using Nox.UI;
using Nox.UI.Widgets;
using Nox.Users;
using UnityEngine;

namespace api.nox.server {
	public class Client : IClientModInitializer {
		internal static IUiAPI UiAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("ui")
				.GetInstance<IUiAPI>();

		internal static IUserAPI UserAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("users")
				.GetInstance<IUserAPI>();

		internal static INetworkAPI NetworkAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("network")
				.GetInstance<INetworkAPI>();

		public static T GetAsset<T>(ResourceIdentifier path) where T : UnityEngine.Object
			=> Instance.CoreAPI.AssetAPI.GetAsset<T>(path);

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
			if (ServerPage.GetStaticKey() == key)
				page = ServerPage.OnGotoAction(menu, context.Data[2..]);
			if (page == null) return;
			Instance.CoreAPI.EventAPI.Emit("menu_display", menu.Id, page);
		}

		private void OnWidgetRequest(EventData context) {
			if (!context.TryGet(0, out int mid)) return;
			if (!context.TryGet(1, out RectTransform tr)) return;
			var menu = UiAPI?.Get<IMenu>(mid);
			if (menu == null) return;
			List<(GameObject, IWidget)> widgets = new();
			if (HostWidget.TryMake(menu, tr, out var widget))
				widgets.Add(widget);
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