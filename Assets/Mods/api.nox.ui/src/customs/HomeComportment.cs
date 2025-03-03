using api.nox.ui.components;
using api.nox.ui.widgets;
using Nox.CCK.Utils;
using UnityEngine;
using Transform = UnityEngine.Transform;

namespace api.nox.ui.pages
{
    public class HomeComportment : MonoBehaviour
    {
        public WidgetGrid grid;

        public void UpdateWidgets(Widget[] widgets)
        {
            foreach (Transform child in grid.transform)
                Destroy(child.gameObject);
            
            var rect = grid.GetComponent<RectTransform>();
            
            foreach (var widget in widgets)
            {
                var go = widget.GetContent(rect);
                var gi = go.GetComponent<WidgetGridItem>();
                gi.size = widget.Size;
            }

            ForceUpdateLayout.UpdateManually(grid.gameObject);
        }
    }
}