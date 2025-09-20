using api.nox.world.client;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.Widgets;
using Nox.Users;
using UnityEngine;
using UnityEngine.UI;

namespace api.nox.world.widget {
	public class HomeWidget : MonoBehaviour, IWidget {
		public static string GetDefaultKey()
			=> "home";

		private int               _mid;
		private Image             _image;
		private AspectRatioFitter _ratio;
		private GameObject        _container;
		private GameObject        _content;

		private void OnClick()
			=> Client.UiAPI?.SendGoto(_mid, WorldPage.GetStaticKey(), "identifier", GetHomeIdentifier());

		public string GetKey()
			=> GetDefaultKey();

		public Vector2Int GetSize()
			=> Vector2Int.one;

		public int GetPriority()
			=> 99;

		internal static WorldIdentifier GetHomeIdentifier(ICurrentUser current = null)
			=> WorldIdentifier.FromString((current ?? Main.Instance.UserAPI.GetCurrent())?.GetHomeId());

		public async UniTask UpdateContent() {
			var identifier = GetHomeIdentifier();
			if (!(identifier?.IsValid() ?? false)) {
				_container.SetActive(false);
				return;
			}

			if (!_image.sprite)
				_container.SetActive(false);

			var home = await Main.Instance.Network.Fetch(identifier);
			if (home == null || string.IsNullOrEmpty(home.GetThumbnailUrl())) {
				_container.SetActive(false);
				return;
			}

			var thumbnail = await Main.Instance.NetworkAPI.FetchTexture(home.GetThumbnailUrl());
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
			var icon      = await Client.GetAssetAsync<Sprite>("icons/home.png", "ui");
			var labelIcon = Reference.GetComponent<Image>("icon", _content);
			labelIcon.sprite = icon;
		}
	}
}