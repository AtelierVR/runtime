using api.nox.world.client;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.Widgets;
using Nox.Users;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace api.nox.world.widget {
	public class HomeWidget : MonoBehaviour, IWidget {
		public static string GetDefaultKey()
			=> "home";

		private int _mid;

		private void OnClick()
			=> Client.UiAPI?.SendGoto(_mid, WorldPage.GetStaticKey(), "identifier", GetHomeIdentifier());

		public IMenu GetMenu()
			=> Client.UiAPI.Get<IMenu>(_mid);

		public string GetKey()
			=> GetDefaultKey();

		public Vector2Int GetSize()
			=> Vector2Int.one;

		public int GetPriority()
			=> 99;

		internal static WorldIdentifier GetHomeIdentifier(ICurrentUser current = null)
			=> WorldIdentifier.FromString((current ?? Main.Instance.UserAPI.GetCurrent())?.GetHomeId());

		public static bool TryMake(IMenu menu, RectTransform parent, out (GameObject, IWidget) values) {
			if (!(GetHomeIdentifier()?.IsValid() ?? false)) {
				values = (null, null);
				return false;
			}

			var prefab    = Client.GetAsset<GameObject>("prefabs/grid_item.prefab", "ui");
			var instance  = Instantiate(prefab, parent);
			var component = instance.AddComponent<HomeWidget>();
			component._mid = menu.GetId();
			var button = Reference.GetComponent<Button>("button", instance);
			button.onClick.AddListener(component.OnClick);
			instance.name = $"[{component.GetKey()}_{instance.GetInstanceID()}]";
			values        = (instance, component);
			return true;
		}
	}
}