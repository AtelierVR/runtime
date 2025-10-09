using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Events;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.Widgets;
using UnityEngine;
using UnityEngine.UI;

namespace api.nox.session.widget {
	public class RespawnWidget : MonoBehaviour, IWidget {
		public static string GetDefaultKey()
			=> "respawn";

		private GameObject _content;

		public static void OnRespawn(EventData context)
			=> OnRespawn();

		public static void OnRespawn()
			=> Main.Instance.GetCurrent()?.GetAdapter()?.GetLocalPlayer()?.Respawn();

		public string GetKey()
			=> GetDefaultKey();

		public Vector2Int GetSize()
			=> Vector2Int.one;

		public int GetPriority()
			=> 100;

		public static bool TryMake(IMenu menu, RectTransform parent, out (GameObject, IWidget) values) {
			var prefab    = Client.GetAsset<GameObject>("prefabs/grid_item.prefab", "ui");
			var instance  = Instantiate(prefab, parent);
			var component = instance.AddComponent<RespawnWidget>();
			var button    = Reference.GetComponent<Button>("button", instance);
			button.onClick.AddListener(OnRespawn);
			instance.name      = $"[{component.GetKey()}_{instance.GetInstanceID()}]";
			values             = (instance, component);
			prefab             = Client.GetAsset<GameObject>("prefabs/widget.prefab", "ui");
			component._content = Instantiate(prefab, Reference.GetComponent<RectTransform>("content", instance));
			component.UpdateIcon().Forget();
			return true;
		}

		private async UniTask UpdateIcon() {
			var icon      = await Client.GetAssetAsync<Sprite>("icons/flag.png", "ui");
			var labelIcon = Reference.GetComponent<Image>("icon", _content);
			labelIcon.sprite = icon;
		}
	}
}