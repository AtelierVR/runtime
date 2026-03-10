using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.UI;
using UnityEngine;

namespace api.nox.ui.layouts {
	public class Specials : Part {
		public RectTransform backContainer;

		public override string GetKey()
			=> "specials";

		override protected UniTask<GameObject> GetPrefab()
			=> PageManager.GetAssetAsync<GameObject>("buttons/special.prefab");

		private static UniTask<GameObject> GetBack()
			=> PageManager.GetAssetAsync<GameObject>("buttons/special_back.prefab");

		public override UniTask AddElement(NavigationData element, GameObject pefab = null)
			=> AddElementBack(element, pefab, true);

		private async UniTask AddElementBack(NavigationData element, GameObject pefab = null, bool updateBacks = true) {
			await base.AddElement(element, pefab);
			if (updateBacks) await UpdateBacks();
		}

		public override async UniTask AddElements(NavigationData[] elements) {
			var prefab = await GetPrefab();
			await UniTask.WhenAll(elements.Select(element => AddElementBack(element, prefab, false)));
			await UpdateBacks();
		}

		public override void RemoveElement(string key) {
			base.RemoveElement(key);
			UpdateBacks().Forget();
		}

		private async UniTask UpdateBacks() {
			// present backs
			var present = GetChildren();
			var keys    = new HashSet<int>();

			foreach (var entry in present)
				keys.Add(entry.GetInstanceID());
			var prefab = await GetBack();

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