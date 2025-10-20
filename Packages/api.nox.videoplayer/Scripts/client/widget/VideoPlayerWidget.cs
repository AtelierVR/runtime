using System.Linq;
using api.nox.videoplayer.client;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.Widgets;
using UnityEngine;
using UnityEngine.UI;

namespace api.nox.videoplayer.widget {
	public class VideoPlayerWidget : MonoBehaviour, IWidget {
		public static string GetDefaultKey()
			=> "videoPlayer";

		public string GetKey()
			=> GetDefaultKey();

		private int               _mid;
		private GameObject        _content;
		private RawImage          _image;
		private AspectRatioFitter _ratio;
		private GameObject        _container;

		private void OnClick()
			=> Client.UiAPI?.SendGoto(_mid, VideoPlayerPage.GetStaticKey());

		public Vector2Int GetSize()
			=> Vector2Int.one;

		public int GetPriority()
			=> 100;

		public static bool TryMake(IMenu menu, RectTransform parent, out (GameObject, IWidget) values) {
			var prefab    = Client.GetAsset<GameObject>("prefabs/grid_item.prefab", "ui");
			var instance  = Instantiate(prefab, parent);
			var component = instance.AddComponent<VideoPlayerWidget>();
			component._mid = menu.GetId();

			var button = Reference.GetComponent<Button>("button", instance);
			button.onClick.AddListener(component.OnClick);
			instance.name = $"[{component.GetKey()}_{instance.GetInstanceID()}]";
			values        = (instance, component);

			prefab             = Client.GetAsset<GameObject>("prefabs/widget_image.prefab", "ui");
			component._content = Instantiate(prefab, Reference.GetComponent<RectTransform>("content", instance));


			var image = Reference.GetComponent<Image>("image", component._content);
			component._image = image.GetOrAddComponent<RawImage>();
			image.Destroy();
			component._ratio     = Reference.GetComponent<AspectRatioFitter>("ratio", component._content);
			component._container = Reference.GetReference("image_container", component._content);

			component.UpdateIcon().Forget();

			return true;
		}

		public void Update() {
			var videoplayer = VideoPlayerManager.VideoPlayers.FirstOrDefault();
			if (videoplayer == null) {
				_container.SetActive(false);
				return;
			}

			var texture = videoplayer.GetRender();
			if (!texture || texture.height == 0) {
				_container.SetActive(false);
				return;
			}

			if (_image.texture != texture)
				_image.texture = texture;

			var aspect = (float)texture.width / texture.height;
			if (!Mathf.Approximately(_ratio.aspectRatio, aspect))
				_ratio.aspectRatio = aspect;

			_container.SetActive(true);
		}

		private async UniTask UpdateIcon() {
			var icon      = await Client.GetAssetAsync<Sprite>("icons/play_arrow.png", "ui");
			var labelIcon = Reference.GetComponent<Image>("icon", _content);
			labelIcon.sprite = icon;
		}
	}
}