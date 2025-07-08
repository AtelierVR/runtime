using UnityEngine;

namespace api.nox.ui.layouts {
	public class Favorites : Part {
		public override string GetKey()
			=> "favorites";

		public override GameObject GetPrefab()
			=> PageManager.GetAsset<GameObject>("buttons/favorite.prefab");
	}
}