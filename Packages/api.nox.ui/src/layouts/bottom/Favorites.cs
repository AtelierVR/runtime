using Cysharp.Threading.Tasks;
using UnityEngine;

namespace api.nox.ui.layouts {
	public class Favorites : Part {
		public override string GetKey()
			=> "favorites";

		public override UniTask<GameObject> GetPrefab()
			=> PageManager.GetAssetAsync<GameObject>("buttons/favorite.prefab");
	}
}