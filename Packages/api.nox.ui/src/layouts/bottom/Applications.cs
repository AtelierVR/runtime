using System.Linq;
using Nox.UI;
using UnityEngine;

namespace api.nox.ui.layouts {
	public class Applications : Part {
		
		public override string GetKey()
			=> "applications";

		public override GameObject GetPrefab()
			=> PageManager.GetAsset<GameObject>("buttons/application.prefab");
	}
}