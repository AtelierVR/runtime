using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.UI;
using UnityEngine;

namespace api.nox.ui.layouts {
	public class Applications : Part {
		public override string GetKey()
			=> "applications";

		public override async UniTask<GameObject> GetPrefab()
			=> await PageManager.GetAssetAsync<GameObject>("buttons/application.prefab");
	}
}