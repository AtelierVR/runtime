/*using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;
using Transform = UnityEngine.Transform;

namespace api.nox.game.Tiles
{
    public class WidgetManager : IDisposable
    {
        private EventSubscription sub;

        internal WidgetManager()
        {
            sub = GameClientSystem.CoreAPI.EventAPI.Subscribe("game.widget", OnWidget);
        }

        internal void UpdateWidgets(int menuId, GameObject content)
        {
            var rect = Reference.GetReference("game.home.widgets", content).GetComponent<WidgetGrid>();
            foreach (var child in rect.transform.Cast<Transform>().ToArray())
                Object.Destroy(child.gameObject);
            foreach (var widget in widgets.Values.ToArray())
            {
                var go = widget.GetContent(menuId, rect.transform);
                var gi = go.GetComponent<MenuGridderItem>();
                gi.size = new Vector2Int((int)widget.width, (int)widget.height);
            }

            ForceUpdateLayout.UpdateManually(rect.GetComponent<RectTransform>());
        }

        private Dictionary<string, HomeWidget> widgets = new();

        private void OnWidget(EventData context)
        {
            if (widgets == null) return;
            var widget = (context.Data[0] as ShareObject)?.Convert<HomeWidget>();
            if (widget == null || string.IsNullOrEmpty(widget.id)) return;
            if (widgets.ContainsKey(widget.id) && widget.GetContent == null)
            {
                widgets.Remove(widget.id);
                OnWidgetRemove.Invoke(widget);
                return;
            }

            if (widget.GetContent == null) return;
            if (!widgets.TryAdd(widget.id, widget))
            {
                widgets[widget.id] = widget;
                OnWidgetUpdate.Invoke(widget);
            }
            else OnWidgetAdd.Invoke(widget);

            OnWidgetsUpdate.Invoke(widgets.Values.ToArray());
        }

        public void Dispose()
        {
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(sub);
            OnWidgetsUpdate.RemoveAllListeners();
            OnWidgetAdd.RemoveAllListeners();
            OnWidgetRemove.RemoveAllListeners();
            OnWidgetUpdate.RemoveAllListeners();
            widgets.Clear();
            widgets = null;
            OnWidgetsUpdate = null;
            OnWidgetAdd = null;
            OnWidgetRemove = null;
            OnWidgetUpdate = null;
        }

        internal class OnWidgetEvent : UnityEvent<HomeWidget>
        {
        }

        internal class OnWidgetsEvent : UnityEvent<HomeWidget[]>
        {
        }

        internal OnWidgetsEvent OnWidgetsUpdate = new();
        internal OnWidgetEvent OnWidgetAdd = new();
        internal OnWidgetEvent OnWidgetRemove = new();
        internal OnWidgetEvent OnWidgetUpdate = new();
    }
}*/