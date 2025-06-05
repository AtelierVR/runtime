using UnityEngine;

namespace api.nox.ui.layouts {
	public class Actions : Part {
		public override string GetKey()
			=> "actions";

		public override GameObject GetPrefab()
			=> PageManager.GetAsset<GameObject>("buttons/action.prefab");
	}
}