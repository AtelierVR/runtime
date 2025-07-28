using Nox.UI;
using Nox.UI.Widgets;
using UnityEngine;

namespace api.nox.user.widget {
	public class UserWidget : MonoBehaviour, IWidget {
		public static string GetDefaultKey()
			=> "current_user";

		public string GetKey()
			=> GetDefaultKey();

		public Vector2Int GetSize()
			=> new(3, 2);

		public int GetPriority()
			=> 100;

		public static bool TryMake(IMenu menu, RectTransform parent, out (GameObject, IWidget) values) {
			var prefab    = Client.GetAsset<GameObject>("prefabs/grid_item.prefab", "ui");
			var instance  = Instantiate(prefab, parent);
			var component = instance.AddComponent<UserWidget>();
			instance.name = $"[{component.GetKey()}_{instance.GetInstanceID()}]";
			values        = (instance, component);
			return true;
		}
	}
}