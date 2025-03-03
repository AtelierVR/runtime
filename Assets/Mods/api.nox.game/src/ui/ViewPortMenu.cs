using System;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.game.UI
{
    public class ViewPortMenu : Menu
    {
        public override void Dispose()
        {
            base.Dispose();
            Destroy(gameObject);
        }

        public void OnDestroy()
        {
            Logger.Log("Destroying ViewPortMenu");
        }

        public MenuFormat[] formats = Array.Empty<MenuFormat>();

        public void SetFormat(string key)
        {
            foreach (var format in formats)
                if (format.key == key)
                {
                    var rect = GetComponent<RectTransform>();
                    var parent = rect.parent.GetComponent<RectTransform>();
                    // change only the value sizeDelta.x and use the height
                    var height = parent.rect.height;
                    var width = parent.rect.width;
                    
                    Logger.Log($"Setting format {key} ({format.ratio}) with ({width}x{height})");

                    if (height < width)
                    {
                        var newX = height * format.ratio;
                        rect.sizeDelta = new Vector2(newX, 0);
                    }
                    else
                    {
                        var newY = width * format.ratio;
                        rect.sizeDelta = new Vector2(0, newY);
                    }
                }
        }
    }

    [Serializable]
    public class MenuFormat
    {
        public string key;
        public float ratio;
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(ViewPortMenu))]
    public class ViewPortMenuEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var menu = (ViewPortMenu)target;
            foreach (var format in menu.formats)
                if (GUILayout.Button($"Set format {format.key}"))
                    menu.SetFormat(format.key);
        }
    }
#endif
}