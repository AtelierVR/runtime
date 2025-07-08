using System.Collections.Generic;
using System.Linq;
using Nox.UI;
using UnityEngine;

namespace api.nox.ui.layouts {
	public class Specials : Part {
		public RectTransform backContainer;

		public override string GetKey()
			=> "specials";

		public override GameObject GetPrefab()
			=> PageManager.GetAsset<GameObject>("buttons/special.prefab");

		private GameObject GetBack()
			=> PageManager.GetAsset<GameObject>("buttons/special_back.prefab");

		public override void AddElement(NavigationData element) {
			base.AddElement(element);
			UpdateBacks();
		}

		public override void RemoveElement(string key) {
			base.RemoveElement(key);
			UpdateBacks();
		}

		private void UpdateBacks() {
			// present backs
			var present = GetChildren();
			var keys    = new HashSet<int>();

			foreach (var entry in present)
				keys.Add(entry.GetInstanceID());

			// add backs for each present element
			foreach (var entry in present) {
				if (backContainer.Find(entry.GetInstanceID().ToString("x8"))) continue;
				var back = Instantiate(GetBack(), backContainer);
				back.name = entry.GetInstanceID().ToString("x8");
			}

			// remove backs for each absent element
			foreach (RectTransform entry in backContainer) {
				if (keys.Contains(int.Parse(entry.name, System.Globalization.NumberStyles.HexNumber))) continue;
				#if UNITY_EDITOR
				if (Application.isPlaying) Destroy(entry.gameObject);
				else UnityEditor.EditorApplication.delayCall += () => DestroyImmediate(entry.gameObject);
				#else
				Destroy(entry.gameObject);
				#endif
			}
		}
	}
}