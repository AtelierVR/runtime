#if UNITY_EDITOR
using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using UnityEditor;
using UnityEngine;

namespace api.nox.ui {
	public class Editor : EditorModInitializer {
		internal static EditorModCoreAPI CoreAPI;

		public void OnInitializeEditor(EditorModCoreAPI api) {
			CoreAPI                            =  api;
			EditorApplication.hierarchyChanged += OnHierarchyChanged;
			OnHierarchyChanged();
		}

		public void OnDisposeEditor() {
			EditorApplication.hierarchyChanged -= OnHierarchyChanged;
		}

		private void OnHierarchyChanged() {
			if (EditorApplication.isPlaying) return;
			var canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
			foreach (var c in canvas.Where(e => e.gameObject.activeInHierarchy))
				UpdateLayout.UpdateManually(c.gameObject);
		}
	}
}
#endif