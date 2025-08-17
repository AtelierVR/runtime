#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Nox.CCK.Utils {
	public static class Layers {
		public static bool LayerExists(string name)
			=> LayersExists(new[] { name });

		public static bool LayersExists(string[] names) {
			var layers = GetLayers().Keys;
			return layers.All(name => layers.Contains(name));
		}

		public static void CreateLayers(string[] names) {
			var created = false;
			var li      = GetLayers();
			var manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
			var layers  = manager.FindProperty("layers");
			foreach (var name in names) {
				if (li.ContainsKey(name)) continue;
				for (var i = 0; i < 31; i++) {
					var el = layers.GetArrayElementAtIndex(i);
					if (!string.IsNullOrEmpty(el.stringValue) || i < 6) continue;
					el.stringValue = name;
					created        = true;
					break;
				}
			}

			if (!created)
				return;

			manager.ApplyModifiedProperties();
			Logger.LogDebug($"{names.Length} layers created: {string.Join(", ", names)}");
		}

		public static void CreateLayer(string name)
			=> CreateLayers(new[] { name });

		public static Dictionary<string, int> GetLayers() {
			var results = new Dictionary<string, int>();
			var manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
			var layers  = manager.FindProperty("layers");
			var size    = layers.arraySize;

			for (var i = 0; i < size; i++) {
				var el   = layers.GetArrayElementAtIndex(i);
				var name = el.stringValue;
				if (!string.IsNullOrEmpty(name))
					results.Add(name, i);
			}

			return results;
		}
	}
}
#endif