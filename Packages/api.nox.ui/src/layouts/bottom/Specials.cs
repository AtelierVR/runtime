using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Utils;
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

		public override void AddElement(NavigationData element, GameObject pefab = null)
			=> AddElementBack(element, pefab, true);

		public void AddElementBack(NavigationData element, GameObject pefab = null, bool updateBacks = true) {
			base.AddElement(element, pefab);
			if (updateBacks) UpdateBacks();
		}

		public override void AddElements(NavigationData[] elements) {
			var prefab = GetPrefab();
			foreach (var data in elements)
				AddElementBack(data, prefab, false);
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
			var prefab = GetBack();

			// add backs for each present element
			foreach (var entry in present) {
				if (backContainer.Find(entry.GetInstanceID().ToString("x8"))) continue;
				var back = Instantiate(prefab, backContainer);
				back.name = entry.GetInstanceID().ToString("x8");
			}

			// remove backs for each absent element
			foreach (RectTransform entry in backContainer) {
				if (keys.Contains(int.Parse(entry.name, System.Globalization.NumberStyles.HexNumber))) continue;
				entry.gameObject.Destroy();
			}
		}
	}
}