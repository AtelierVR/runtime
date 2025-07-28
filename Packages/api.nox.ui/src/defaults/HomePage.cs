using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.ui.components;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Mods.Events;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.Widgets;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace api.nox.ui.defaults {
	public class HomePage : IPage {
		public static string GetStaticKey()
			=> "home";

		private readonly int        _mId;
		private readonly object[]   _context;
		private          GameObject _content;

		private RectTransform       _notificationContent;
		private RectTransform       _dashboardContent;
		private RectTransform       _friendsContent;
		private RectTransform       _widgetContent;
		private GameObject          _widgetPrefab;
		private EventSubscription[] _events = Array.Empty<EventSubscription>();

		public static IPage OnGotoAction(IMenu menu, object[] o)
			=> new HomePage(menu.GetId(), o);

		private HomePage(int mId, object[] context) {
			_mId     = mId;
			_context = context;
		}

		public string GetKey()
			=> GetStaticKey();

		public object[] GetContext()
			=> _context;

		public UniTask<GameObject> GetContentAsync(RectTransform parent)
			=> UniTask.FromResult(GetContent(parent));

		public IMenu GetMenu()
			=> Client.Instance.Get<IMenu>(_mId);

		public GameObject GetContent(RectTransform parent) {
			if (_content) return _content;
			_content      = Object.Instantiate(PageManager.GetAsset<GameObject>("prefabs/split.prefab"), parent);
			_content.name = $"[{GetStaticKey()}_{_content.GetInstanceID()}]";
			var splitContent     = Reference.GetComponent<RectTransform>("content", _content);
			var containerAsset   = PageManager.GetAsset<GameObject>("prefabs/container.prefab");
			var withTitleAsset   = PageManager.GetAsset<GameObject>("prefabs/with_title.prefab");
			var iconAsset        = PageManager.GetAsset<GameObject>("prefabs/header_icon.prefab");
			var labelAsset       = PageManager.GetAsset<GameObject>("prefabs/header_label.prefab");
			var scrollAsset      = PageManager.GetAsset<GameObject>("prefabs/scroll.prefab");
			var widgetGroupAsset = PageManager.GetAsset<GameObject>("prefabs/grid_group.prefab");
			var boxAsset         = PageManager.GetAsset<GameObject>("prefabs/box.prefab");
			_widgetPrefab = PageManager.GetAsset<GameObject>("prefabs/grid_item.prefab");

			// generate background containers

			// generate notification
			var container = Object.Instantiate(containerAsset, splitContent);
			var withTitle = Object.Instantiate(withTitleAsset, Reference.GetComponent<RectTransform>("content", container));
			var header    = Reference.GetReference("header", withTitle);
			var icon      = Object.Instantiate(iconAsset, Reference.GetComponent<RectTransform>("before", header));
			var label     = Object.Instantiate(labelAsset, Reference.GetComponent<RectTransform>("content", header));

			Reference.GetComponent<Image>("image", icon).sprite = PageManager.GetAsset<Sprite>("icons/notifications.png");
			Reference.GetComponent<TextLanguage>("text", label).UpdateText("notifications.title");
			_notificationContent = Reference.GetComponent<RectTransform>("content", withTitle);

			// generate dashboard
			container = Object.Instantiate(PageManager.GetAsset<GameObject>("prefabs/container_full.prefab"), splitContent);
			withTitle = Object.Instantiate(withTitleAsset, Reference.GetComponent<RectTransform>("content", container));
			header    = Reference.GetReference("header", withTitle);
			icon      = Object.Instantiate(iconAsset, Reference.GetComponent<RectTransform>("before", header));
			label     = Object.Instantiate(labelAsset, Reference.GetComponent<RectTransform>("content", header));

			Reference.GetComponent<Image>("image", icon).sprite = PageManager.GetAsset<Sprite>("icons/dashboard.png");
			Reference.GetComponent<TextLanguage>("text", label).UpdateText("dashboard.title");

			container         = Object.Instantiate(scrollAsset, Reference.GetComponent<RectTransform>("content", withTitle));
			_dashboardContent = Reference.GetComponent<RectTransform>("content", container);

			// generate dashboard content

			// wigets
			// box > widget_group > widget_item[]
			var box   = Object.Instantiate(boxAsset, _dashboardContent);
			var group = Object.Instantiate(widgetGroupAsset, Reference.GetComponent<RectTransform>("content", box));
			_widgetContent = Reference.GetComponent<RectTransform>("content", group);
			Reference.GetComponent<TextLanguage>("text", box).UpdateText("widgets.title");

			// generate friends
			container = Object.Instantiate(containerAsset, splitContent);
			withTitle = Object.Instantiate(withTitleAsset, Reference.GetComponent<RectTransform>("content", container));
			header    = Reference.GetReference("header", withTitle);
			icon      = Object.Instantiate(iconAsset, Reference.GetComponent<RectTransform>("before", header));
			label     = Object.Instantiate(labelAsset, Reference.GetComponent<RectTransform>("content", header));

			Reference.GetComponent<Image>("image", icon).sprite = PageManager.GetAsset<Sprite>("icons/friend.png");
			Reference.GetComponent<TextLanguage>("text", label).UpdateText("friends.title");
			_friendsContent = Reference.GetComponent<RectTransform>("content", withTitle);

			return _content;
		}

		public void OnRemove() {
			foreach (var e in _events)
				Client.Instance.CoreAPI.EventAPI.Unsubscribe(e);
			_events = Array.Empty<EventSubscription>();

			_content             = null;
			_notificationContent = null;
			_dashboardContent    = null;
			_friendsContent      = null;
			_widgetContent       = null;
			_widgetPrefab        = null;
		}

		public void OnOpen(IPage lastPage) {
			_events = new[] {
				Client.Instance.CoreAPI.EventAPI.Subscribe("widget_added", AddWidget),
				Client.Instance.CoreAPI.EventAPI.Subscribe("widget_removed", RemoveWidget),
			};
			RequestWidgets();
		}

		public void OnDisplay(IPage lastPage) 
			=> UpdateLayout.UpdateManually(_content);

		private void RemoveWidget(EventData data) {
			if (!_widgetContent || !_widgetPrefab) return;
			if (!data.TryGet(0, out string key)) return;
			var widgets = _widgetContent.GetComponentsInChildren<IWidget>(true)
				.Where(w => w.GetKey() == key)
				.ToArray();
			foreach (var widget in widgets)
				if (widget is Object o)
					Object.Destroy(o);
		}

		public void RequestWidgets() {
			if (!_widgetContent || !_widgetPrefab) return;

			List<IWidget> widgets = new();

			foreach (var widget in _widgetContent.GetComponentsInChildren<IWidget>(true))
				if (widget is Object o)
					Object.Destroy(o);

			Client.Instance.CoreAPI.EventAPI.Emit(
				"widget_request",
				_mId,
				_widgetContent,
				new Action<object[]>(Callback)
			);

			foreach (var widget in widgets.Where(widget => widget != null))
				AddWidget(widget);
			UpdateGridder();

			return;

			void Callback(object[] args) {
				if (args is { Length: 2 } && args[0] is IWidget widget)
					widgets.Add(widget);
			}
		}

		private void AddWidget(EventData data) {
			if (!data.TryGet(0, out IWidget widget)) return;
			AddWidget(widget);
			UpdateGridder();
		}

		private void UpdateGridder() {
			var widgets = _widgetContent.GetComponentsInChildren<IWidget>(true).ToList();
			widgets.Sort((b, a) => a.GetPriority().CompareTo(b.GetPriority()));

			for (var i = 0u; i < widgets.Count; i++) {
				var widget = widgets[(int)i];
				if (widget is not MonoBehaviour mb || !mb.TryGetComponent<WidgetGridItem>(out var item))
					continue;
				item.size  = widget.GetSize();
				item.index = i;
			}

			UpdateLayout.UpdateManually(_dashboardContent);
		}

		private void AddWidget(IWidget widget) {
			if (!_widgetContent || !_widgetPrefab) return;
			if (string.IsNullOrEmpty(widget.GetKey())) return;
			var listExisting = _widgetContent
				.GetComponentsInChildren<IWidget>(true)
				.Where(
					widget1 => widget1.GetKey() == widget.GetKey()
						&& (widget is Object widgeto && widget1 is Object wo
							? wo.GetInstanceID() != widgeto.GetInstanceID()
							: widget1            != widget)
				);
			foreach (var existing in listExisting)
				if (existing is Object o)
					Object.Destroy(o);
			if (widget is not MonoBehaviour w) return;
			var item = w.GetComponent<WidgetGridItem>();
			item.size = widget.GetSize();
		}


		public override string ToString()
			=> $"{GetType().Name}[Key={GetKey()}, MenuId={_mId}, Context=[{string.Join(", ", _context)}]]";
	}
}