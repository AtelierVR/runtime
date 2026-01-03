using Cysharp.Threading.Tasks;
using UnityEngine;

namespace api.nox.ui.layouts {
	public class Actions : Part {
		public override string GetKey()
			=> "actions";

		public override UniTask<GameObject> GetPrefab()
			=> PageManager.GetAssetAsync<GameObject>("buttons/action.prefab");
	}
}