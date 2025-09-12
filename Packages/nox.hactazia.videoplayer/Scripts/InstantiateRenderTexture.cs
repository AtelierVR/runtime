using UnityEngine;
using UnityEngine.UI;

namespace Nox.Hactazia.VideoPlayer {
	public class InstantiateRenderTexture : MonoBehaviour {
		public NhVideoPlayer player;
		public RawImage      image;

		private void Awake() {
			player ??= GetComponent<NhVideoPlayer>();
			image  ??= GetComponent<RawImage>();

			if (!player || !image) {
				enabled = false;
				return;
			}

			var render = player.render ? Duplicate() : Create();
			image.texture = render;
			player.render = render;
		}

		private RenderTexture Create() {
			var render = new RenderTexture(1920, 1080, 0) { format = RenderTextureFormat.ARGB32 };
			render.Create();
			return render;
		}

		private RenderTexture Duplicate() {
			var render = Instantiate(player.render);
			render.Create();
			return render;
		}
	}
}