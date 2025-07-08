using UnityEngine;

namespace api.nox.ui.layouts {
	public class Histories : Part {
		public override string GetKey()
			=> "histories";

		public override GameObject GetPrefab()
			=> PageManager.GetAsset<GameObject>("buttons/history.prefab");
	}
}