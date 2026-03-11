using api.nox.user.client;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.Widgets;
using UnityEngine;
using UnityEngine.UI;

namespace api.nox.user.widget {
	public class AuthWidget : MonoBehaviour, IWidget {
		public static string GetDefaultKey()
			=> "auth_login";

		public string GetKey()
			=> GetDefaultKey();

		private int          _mid;
		private Image        _icon;
		private TextLanguage _label;

		private void OnClick()
			=> Client.UiAPI?.SendGoto(
				_mid, AuthServerPage.GetStaticKey()
			);

		public Vector2Int GetSize()
			=> new(2, 2);

		public int GetPriority()
			=> 99;

		public static bool TryMake(IMenu menu, RectTransform parent, out (GameObject, IWidget) values) {
			// Only show when user is NOT logged in (inverse of UserWidget)
			if (Main.Instance.Network.CurrentUser != null) {
				values = (null, null);
				return false;
			}

			var prefab    = Client.GetAsset<GameObject>("ui:prefabs/grid_item.prefab");
			var instance  = prefab.Instantiate(parent);
			var component = instance.AddComponent<AuthWidget>();
			component._mid = menu.GetId();

			var button = Reference.GetComponent<Button>("button", instance);
			button.onClick.AddListener(component.OnClick);
			instance.name = $"[{component.GetKey()}_{instance.GetInstanceID()}]";
			values        = (instance, component);

			prefab = Client.GetAsset<GameObject>("ui:prefabs/widget.prefab");
			var content = Object.Instantiate(prefab, Reference.GetComponent<RectTransform>("content", instance));
			component._icon = Reference.GetComponent<Image>("icon", content);

			component.UpdateContent().Forget();

			return true;
		}

		private async UniTask UpdateContent()
			=> _icon.sprite = await Client.GetAssetAsync<Sprite>("ui:icons/login.png");
	}
}