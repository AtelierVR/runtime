using System.Collections.Generic;
using api.nox.ui.components;
using api.nox.ui.widgets;
using UnityEngine;
using Transform = UnityEngine.Transform;

namespace api.nox.ui.pages
{
    public class HomeComportment : MonoBehaviour
    {
        public WidgetGrid grid;

        public void UpdateWidgets(Page page, Widget[] widgets)
        {
            var rect = grid.GetComponent<RectTransform>();
            var items = new List<WidgetGridItem>();

            foreach (Transform child in rect)
                Destroy(child.gameObject);

            foreach (var widget in widgets)
            {
                var go = widget.GetContent(page.MenuId, rect);
                var gi = go.GetComponent<WidgetGridItem>();
                items.Add(gi);
                gi.size = widget.Size;
                gi.position = Vector2Int.zero;
            }

            grid.UpdateContent(items.ToArray());
        }
    }
}