using api.nox.user.client;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.Widgets;
using Nox.Users;
using UnityEngine;
using UnityEngine.UI;

namespace api.nox.user.widget {
	public class UserWidget : MonoBehaviour, IWidget {
		public static string GetDefaultKey()
			=> "current_user";

		public string GetKey()
			=> GetDefaultKey();


		private int               _mid;
		private Image             _image;
		private AspectRatioFitter _ratio;
		private GameObject        _container;
		private GameObject        _content;
		private Image             _icon;
		private TextLanguage      _label;

		private void OnClick()
			=> Client.UiAPI?.SendGoto(_mid, UserPage.GetStaticKey(), "identifier", GetUserIdentifier());

		private static UserIdentifier GetUserIdentifier()
			=> Main.Instance.Network.CurrentUser?.ToInternalIdentifier();

		public Vector2Int GetSize()
			=> new(3, 2);

		public int GetPriority()
			=> 100;

		public static bool TryMake(IMenu menu, RectTransform parent, out (GameObject, IWidget) values) {
			if (!(GetUserIdentifier()?.IsValid() ?? false)) {
				values = (null, null);
				return false;
			}

			var prefab    = Client.GetAsset<GameObject>("prefabs/grid_item.prefab", "ui");
			var instance  = Instantiate(prefab, parent);
			var component = instance.AddComponent<UserWidget>();
			component._mid = menu.GetId();

			var button = Reference.GetComponent<Button>("button", instance);
			button.onClick.AddListener(component.OnClick);
			instance.name = $"[{component.GetKey()}_{instance.GetInstanceID()}]";
			values        = (instance, component);

			prefab               = Client.GetAsset<GameObject>("prefabs/large_widget.prefab", "ui");
			component._content   = Instantiate(prefab, Reference.GetComponent<RectTransform>("content", instance));
			component._image     = Reference.GetComponent<Image>("image", component._content);
			component._ratio     = Reference.GetComponent<AspectRatioFitter>("image_ratio", component._content);
			component._container = Reference.GetReference("image_container", component._content);
			component._icon      = Reference.GetComponent<Image>("icon", component._content);
			component._label     = Reference.GetComponent<TextLanguage>("label", component._content);

			component.UpdateContent().Forget();

			return true;
		}

		private async UniTask UpdateContent() {
			var identifier = GetUserIdentifier();
			if (!(identifier?.IsValid() ?? false)) {
				_container.SetActive(false);
				await UpdateIcon();
				_label.UpdateText("user.not_logged_in");
				return;
			}

			if (!_image.sprite)
				_container.SetActive(false);
			await UpdateIcon();

			if (Main.Instance.Network.CurrentUser is not IUser user || !user.ToIdentifier().Equals(identifier))
				user = await Main.Instance.Network.Fetch(identifier);

			if (user == null) {
				_container.SetActive(false);
				await UpdateIcon();
				_label.UpdateText("user.not_logged_in");
				return;
			}

			_label.UpdateText(
				"value",
				new[] {
					user.GetDisplay()
					?? user.GetUsername()
					?? identifier.ToString()
				}
			);

			await UniTask.WhenAll(
				UpdateBanner(user),
				UpdateThumbnail(user)
			);
		}

		private async UniTask UpdateBanner(IUser user) {
			var url = user.GetBannerUrl();

			if (string.IsNullOrEmpty(url)) {
				_container.SetActive(false);
				return;
			}

			var banner = await Main.Instance.NetworkAPI.FetchTexture(url);
			if (!banner || banner.height == 0) {
				_container.SetActive(false);
				return;
			}

			_image.sprite = Sprite.Create(
				banner,
				new Rect(0, 0, banner.width, banner.height),
				new Vector2(0.5f, 0.5f)
			);
			_ratio.aspectRatio = (float)banner.width / banner.height;
			_container.SetActive(true);
		}

		private async UniTask UpdateThumbnail(IUser user) {
			var url = user.GetThumbnailUrl();

			if (string.IsNullOrEmpty(url)) {
				await UpdateIcon();
				return;
			}

			var thumbnail = await Main.Instance.NetworkAPI.FetchTexture(url);
			if (!thumbnail || thumbnail.height == 0) {
				await UpdateIcon();
				return;
			}

			await UpdateIcon(
				Sprite.Create(
					thumbnail,
					new Rect(0, 0, thumbnail.width, thumbnail.height),
					new Vector2(0.5f, 0.5f)
				)
			);
		}


		private async UniTask UpdateIcon(Sprite icon = null) {
			icon         ??= await Client.GetAssetAsync<Sprite>("icons/person.png", "ui");
			_icon.sprite =   icon;
		}
	}
}