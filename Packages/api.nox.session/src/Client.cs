using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.session.client;
using api.nox.session.client.handlers;
using api.nox.session.widget;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.Instances;
using Nox.Sessions;
using Nox.UI;
using Nox.UI.Widgets;
using UnityEngine;

namespace api.nox.session {
	public class Client : IClientModInitializer {
		internal static IUiAPI UiAPI
			=> Main.Instance.CoreAPI.ModAPI
				.GetMod("ui")
				.GetInstance<IUiAPI>();

		public static T GetAsset<T>(string path, string ns = null) where T : UnityEngine.Object
			=> string.IsNullOrEmpty(ns)
				? Main.Instance.CoreAPI.AssetAPI.GetAsset<T>(path)
				: Main.Instance.CoreAPI.AssetAPI.GetAsset<T>(ns, path);

		public static UniTask<T> GetAssetAsync<T>(string path, string ns = null) where T : UnityEngine.Object
			=> string.IsNullOrEmpty(ns)
				? Main.Instance.CoreAPI.AssetAPI.GetAssetAsync<T>(path)
				: Main.Instance.CoreAPI.AssetAPI.GetAssetAsync<T>(ns, path);

		private EventSubscription[] _events = Array.Empty<EventSubscription>();

		internal static Client           Instance;
		internal        ClientModCoreAPI CoreAPI;

		public void OnInitializeClient(ClientModCoreAPI api) {
			Instance = this;
			CoreAPI  = api;
			_events = new[] {
				CoreAPI.EventAPI.Subscribe("menu_goto", OnGoto),
				CoreAPI.EventAPI.Subscribe("session_handlers_request", OnHandlerRequest),
				CoreAPI.EventAPI.Subscribe("widget_request", OnWidgetRequest),
				CoreAPI.EventAPI.Subscribe("respawn", RespawnWidget.OnRespawn)
			};
		}

		private static void OnGoto(EventData context) {
			if (!context.TryGet(0, out int mid)) return;
			if (!context.TryGet(1, out string key)) return;
			var menu = UiAPI?.Get<IMenu>(mid);
			if (menu == null) return;
			IPage page = null;
			if (SessionPage.GetStaticKey() == key)
				page = SessionPage.OnGotoAction(menu, context.Data[2..]);
			if (page == null) return;
			Main.Instance.CoreAPI.EventAPI.Emit("menu_display", menu.GetId(), page);
		}

		private static void OnWidgetRequest(EventData context) {
			if (!context.TryGet(0, out int mid)) return;
			if (!context.TryGet(1, out RectTransform tr)) return;
			var menu = UiAPI?.Get<IMenu>(mid);
			if (menu == null) return;
			List<(GameObject, IWidget)> widgets = new();
			if (RespawnWidget.TryMake(menu, tr, out var widget))
				widgets.Add(widget);
			foreach (var value in widgets)
				context.Callback(value.Item2, value.Item1);
		}


		private static void OnHandlerRequest(EventData context) {
			if (!context.TryGet(0, out ISession mid)) return;
			context.Callback(
				MainHandler.GetId(),
				MainHandler.GetDisplay(),
				MainHandler.GetIcon()
			);
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