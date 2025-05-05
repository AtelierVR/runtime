#if UNITY_EDITOR
using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using UnityEditor;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.ui
{
    public class UIEditor : EditorModInitializer
    {
        public void OnInitializeEditor(EditorModCoreAPI api)
        {
            Logger.LogDebug($"OnInitializeEditor {nameof(UIEditor)}");
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            OnHierarchyChanged();
        }

        public void OnDisposeEditor()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }

        private void OnHierarchyChanged()
        {
            if (EditorApplication.isPlaying) return;
            var canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in canvas.Where(e => e.gameObject.activeInHierarchy))
                ForceUpdateLayout.UpdateManually(c.gameObject);
        }
    }
}
#endif