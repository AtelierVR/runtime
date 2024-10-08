using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using UnityEngine;

namespace api.nox.game.Tiles
{
    public class WidgetManager : IDisposable
    {
        private EventSubscription sub;

        public void Listen()
        {
            sub = GameClientSystem.CoreAPI.EventAPI.Subscribe("game.widget", OnWidget);
        }

        public void Unlisten()
        {
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(sub);
        }

        internal void UpdateWidgets(int menuId, GameObject content)
        {
            Debug.Log("HomeTileManager.UpdateWidgets");
            var rect = Reference.GetReference("game.home.widgets", content).GetComponent<MenuGridder>();
            foreach (Transform child in rect.transform)
                UnityEngine.Object.Destroy(child.gameObject);
            foreach (var widget in widgets.Values)
            {
                var go = widget.GetContent(menuId, rect.transform);
                var gi = go.GetComponent<MenuGridderItem>();
                gi.size = new Vector2(widget.width, widget.height);
            }
            ForceUpdateLayout.UpdateManually(rect.GetComponent<RectTransform>());
        }

        internal Dictionary<string, HomeWidget> widgets = new();

        private void OnWidget(EventData context)
        {
            Debug.Log("HomeTileManager.OnWidget");
            for (int i = 0; i < context.Data.Length; i++)
                Debug.Log($"context.Data[{i}]: {context.Data[i]}");
            var widget = (context.Data[0] as ShareObject).Convert<HomeWidget>();
            if (widgets.ContainsKey(widget.id) && widget.GetContent == null)
            {
                widgets.Remove(widget.id);
                OnWidgetRemove?.Invoke(widget);
                return;
            }
            if (widget.GetContent == null) return;
            if (widgets.ContainsKey(widget.id))
            {
                widgets[widget.id] = widget;
                OnWidgetUpdate?.Invoke(widget);
            }
            else
            {
                widgets.Add(widget.id, widget);
                OnWidgetAdd?.Invoke(widget);
            }
            OnWidgetsUpdate?.Invoke(widgets.Values.ToArray());
        }

        public void Dispose()
        {
            Unlisten();
            widgets.Clear();
            widgets = null;
            OnWidgetsUpdate = null;
            OnWidgetAdd = null;
            OnWidgetRemove = null;
            OnWidgetUpdate = null;
        }

        internal event Action<HomeWidget[]> OnWidgetsUpdate;
        internal event Action<HomeWidget> OnWidgetAdd;
        internal event Action<HomeWidget> OnWidgetRemove;
        internal event Action<HomeWidget> OnWidgetUpdate;
    }
}