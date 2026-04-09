using api.nox.server.client;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.Widgets;
using Nox.Users;
using UnityEngine;
using UnityEngine.UI;

namespace api.nox.server.widget {
	public class HostWidget : MonoBehaviour, IWidget {
		public static string GetDefaultKey()
			=> "host";

		private int _mid;

		private void OnClick()
			=> Client.UiAPI?.SendGoto(_mid, ServerPage.GetStaticKey(), "address", GetAddress());

		public string GetKey()
			=> GetDefaultKey();

		public Vector2Int GetSize()
			=> Vector2Int.one;

		public int GetPriority()
			=> 70;

		private static string GetAddress(ICurrentUser current = null)
			=> (current ?? Client.UserAPI.Current)?.Server;

		public static bool TryMake(IMenu menu, RectTransform parent, out (GameObject, IWidget) values) {
			if (string.IsNullOrEmpty(GetAddress())) {
				values = (null, null);
				return false;
			}
			var prefab    = Client.GetAsset<GameObject>("ui:prefabs/grid_item.prefab");
			var instance  = Instantiate(prefab, parent);
			var component = instance.AddComponent<HostWidget>();
			component._mid = menu.Id;
			var button = Reference.GetComponent<Button>("button", instance);
			button.onClick.AddListener(component.OnClick);
			instance.name = $"[{component.GetKey()}_{instance.GetEntityId().GetHashCode()}]";
			values        = (instance, component);
			return true;
		}
	}
}