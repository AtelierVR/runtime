using System;
using System.Collections.Generic;
using Nox.CCK.Mods.Events;
using Nox.CCK.Utils;
using UnityEngine.Events;

namespace api.nox.ui.widgets
{
    public class WidgetManager : INoxObject, IDisposable
    {
        internal readonly List<Widget> Cache = new();
        private readonly EventSubscription[] _events;
        public readonly UnityEvent<Widget> OnWidgetAdded = new();
        public readonly UnityEvent<Widget> OnWidgetRemoved = new();
        public readonly UnityEvent<Widget> OnWidgetChanged = new();

        [NoxPublic(NoxAccess.Method)]
        public bool Has(string key) => Cache.Exists(w => w.Key == key);


        [NoxPublic(NoxAccess.Method)]
        public Widget Get(string key) => Cache.Find(w => w.Key == key);

        [NoxPublic(NoxAccess.Method)]
        public void Remove(string key)
        {
            var widget = Get(key);
            if (widget == null) return;
            Cache.Remove(widget);
            UISystem.CoreAPI.EventAPI.Emit("widget_removed", widget);
            OnWidgetRemoved.Invoke(widget);
        }

        [NoxPublic(NoxAccess.Method)]
        public Widget Add(Dictionary<string, object> data) => Add(Widget.From(data));

        private Widget Add(Widget widget)
        {
            if (widget == null) return null;
            if (Has(widget.Key)) return null;
            Cache.Add(widget);
            UISystem.CoreAPI.EventAPI.Emit("widget_added", widget);
            OnWidgetAdded.Invoke(widget);
            return widget;
        }

        [NoxPublic(NoxAccess.Method)]
        public Widget Change(Dictionary<string, object> data) => Change(Widget.From(data));

        private Widget Change(Widget widget)
        {
            if (widget == null) return null;
            var old = Get(widget.Key);
            if (old == null) return null;
            Cache.Remove(old);
            Cache.Add(widget);
            UISystem.CoreAPI.EventAPI.Emit("widget_changed", widget);
            OnWidgetChanged.Invoke(widget);
            return widget;
        }

        public void Dispose()
        {
            foreach (var ev in _events)
                UISystem.CoreAPI.EventAPI.Unsubscribe(ev);
            foreach (var w in Cache)
                Remove(w.Key);
        }
    }
}