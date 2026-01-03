using Cysharp.Threading.Tasks;
using UnityEngine;

namespace api.nox.ui.layouts {
	public class Histories : Part {
		public override string GetKey()
			=> "histories";

		public override UniTask<GameObject> GetPrefab()
			=> PageManager.GetAssetAsync<GameObject>("buttons/history.prefab");
	}
}