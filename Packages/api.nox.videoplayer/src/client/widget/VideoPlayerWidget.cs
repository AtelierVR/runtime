using api.nox.videoplayer.client;
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

		private int _mid;

		private void OnClick()
			=> Client.UiAPI?.SendGoto(_mid, VideoPlayerPage.GetStaticKey());

		public Vector2Int GetSize()
			=> new(3, 2);

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
			return true;
		}
	}
}