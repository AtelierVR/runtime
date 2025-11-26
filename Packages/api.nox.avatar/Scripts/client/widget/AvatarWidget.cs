using api.nox.avatar.client;
using Cysharp.Threading.Tasks;
using Nox.Avatars;
using Nox.Avatars.Controllers;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.Widgets;
using UnityEngine;
using UnityEngine.UI;

namespace api.nox.avatar.widget {
	public class AvatarWidget : MonoBehaviour, IWidget {
		public static string GetDefaultKey()
			=> "avatar";

		private int               _mid;
		private Image             _image;
		private AspectRatioFitter _ratio;
		private GameObject        _container;
		private GameObject        _content;

		private void OnClick()
			=> Client.UiAPI?.SendGoto(_mid, AvatarPage.GetStaticKey(), "identifier", GetCurrentAvatarIdentifier());

		public string GetKey()
			=> GetDefaultKey();

		public Vector2Int GetSize()
			=> Vector2Int.one;

		public int GetPriority()
			=> 98;

		internal static IAvatarIdentifier GetCurrentAvatarIdentifier() {
			var controller = Main.Instance.ControllerAPI?.GetCurrent();
			if (controller is not IControllerAvatar ca)
				return null;

			var currentAvatar = ca.GetAvatar();
			return currentAvatar?.GetIdentifier();
		}

		public async UniTask UpdateContent() {
			var identifier = GetCurrentAvatarIdentifier();
			if (!(identifier?.IsValid() ?? false)) {
				_container.SetActive(false);
				return;
			}

			if (!_image.sprite)
				_container.SetActive(false);

			var avatar = await Main.Instance.Network.Fetch(identifier);
			if (avatar == null || string.IsNullOrEmpty(avatar.GetThumbnailUrl())) {
				_container.SetActive(false);
				return;
			}

			var thumbnail = await Main.Instance.NetworkAPI.FetchTexture(avatar.GetThumbnailUrl());
			if (!thumbnail || thumbnail.height == 0) {
				_container.SetActive(false);
				return;
			}

			_image.sprite = Sprite.Create(
				thumbnail,
				new Rect(0, 0, thumbnail.width, thumbnail.height),
				new Vector2(0.5f, 0.5f)
			);
			_ratio.aspectRatio = (float)thumbnail.width / thumbnail.height;
			_container.SetActive(true);
		}

		public static bool TryMake(IMenu menu, RectTransform parent, out (GameObject, IWidget) values) {
			if (!(GetCurrentAvatarIdentifier()?.IsValid() ?? false)) {
				values = (null, null);
				return false;
			}

			var prefab    = Client.GetAsset<GameObject>("prefabs/grid_item.prefab", "ui");
			var instance  = Instantiate(prefab, parent);
			var component = instance.AddComponent<AvatarWidget>();
			component._mid = menu.GetId();

			var button = Reference.GetComponent<Button>("button", instance);
			button.onClick.AddListener(component.OnClick);
			instance.name = $"[{component.GetKey()}_{instance.GetInstanceID()}]";
			values        = (instance, component);

			prefab               = Client.GetAsset<GameObject>("prefabs/widget_image.prefab", "ui");
			component._content   = Instantiate(prefab, Reference.GetComponent<RectTransform>("content", instance));
			component._image     = Reference.GetComponent<Image>("image", component._content);
			component._ratio     = Reference.GetComponent<AspectRatioFitter>("ratio", component._content);
			component._container = Reference.GetReference("image_container", component._content);

			component.UpdateIcon().Forget();
			component.UpdateContent().Forget();

			return true;
		}

		private async UniTask UpdateIcon() {
			var icon      = await Client.GetAssetAsync<Sprite>("icons/avatar.png", "ui");
			var labelIcon = Reference.GetComponent<Image>("icon", _content);
			labelIcon.sprite = icon;
		}
	}
}
