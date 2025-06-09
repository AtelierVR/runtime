using System.Collections.Generic;
using Nox.CCK.Mods.Events;
using Nox.Widgets;

namespace api.nox.widget {
	public class WidgetManager {
		private readonly List<IWidget>       _widgets = new();
		private readonly Client              _client;
		private readonly EventSubscription[] _events;

		public WidgetManager(Client client) {
			_client = client;
			_events = new[] {
				_client.CoreAPI.EventAPI.Subscribe("widget_set", OnWidgetSet),
			};
		}

		private void OnWidgetSet(EventData context) {
			context.TryGet(0, out IWidget widget);
			if (widget == null) return;
			Set(widget);
		}

		public void Dispose() {
			foreach (var ev in _events)
				_client.CoreAPI.EventAPI.Unsubscribe(ev);
			foreach (var widget in _widgets.ToArray())
				Remove(widget.GetKey());
			_widgets.Clear();
		}

		public bool Has(string key)
			=> _widgets.Exists(w => w.GetKey() == key);

		public T Get<T>(string key) where T : IWidget
			=> (T)_widgets.Find(w => w.GetKey() == key && w is T);

		public void Set(IWidget widget) {
			if (widget == null) return;

			if (Has(widget.GetKey())) {
				var old = Get<IWidget>(widget.GetKey());
				_widgets.Remove(old);
				_widgets.Add(widget);
				_client.CoreAPI.EventAPI.Emit("widget_changed", widget);
				return;
			}

			_widgets.Add(widget);
			_client.CoreAPI.EventAPI.Emit("widget_added", widget);
		}

		public void Remove(string key) {
			var widget = Get<IWidget>(key);
			if (widget == null) return;
			_widgets.Remove(widget);
			_client.CoreAPI.EventAPI.Emit("widget_removed", widget);
		}
	}
}