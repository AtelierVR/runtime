using System;
using System.Collections.Generic;
using System.Linq;
// using api.nox.avatar.client;
// using api.nox.avatar.widget;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.UI;
using Nox.UI.Widgets;
using UnityEngine;

namespace api.nox.avatar {
	public class Client : ClientModInitializer {
		internal static IUiAPI UiAPI
			=> Main.Instance.CoreAPI.ModAPI
				.GetMod("ui")
				.GetClients()
				.FirstOrDefault() as IUiAPI;

		public static T GetAsset<T>(string path, string ns = null) where T : UnityEngine.Object
			=> string.IsNullOrEmpty(ns)
				? Main.Instance.CoreAPI.AssetAPI.GetAsset<T>(path)
				: Main.Instance.CoreAPI.AssetAPI.GetAsset<T>(ns, path);

		private EventSubscription[] _events = Array.Empty<EventSubscription>();

		internal static Client           Instance;
		internal        ClientModCoreAPI CoreAPI;

		public void OnInitializeClient(ClientModCoreAPI api) {
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
			// if (AvatarPage.GetStaticKey() == key)
			// 	page = AvatarPage.OnGotoAction(menu, context.Data[2..]);
			// if (page == null) return;
			Main.Instance.CoreAPI.EventAPI.Emit("menu_display", menu.GetId(), page);
		}

		private void OnWidgetRequest(EventData context) {
			if (!context.TryGet(0, out int mid)) return;
			if (!context.TryGet(1, out RectTransform tr)) return;
			var menu = UiAPI?.Get<IMenu>(mid);
			if (menu == null) return;
			List<(GameObject, IWidget)> widgets = new();
			// if (HomeWidget.TryMake(menu, tr, out var widget))
			// 	widgets.Add(widget);
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