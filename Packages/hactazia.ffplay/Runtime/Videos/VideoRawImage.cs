using Hactazia.FFPlay;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Hactazia.FFPlay {
	[RequireComponent(typeof(RawImage))]
	public class VideoRawImage : MonoBehaviour {
		public VideoWorker worker;

		private RawImage _image;

		private void Start() {
			worker ??= GetComponentInParent<VideoWorker>(true);

			if (!worker) {
				Debug.LogWarning($"No {nameof(VideoWorker)} found.");
				return;
			}

			_image = GetComponent<RawImage>();
			worker.OnDisplay.AddListener(OnDisplay);
		}

		private void OnDestroy()
			=> worker?.OnDisplay.RemoveListener(OnDisplay);

		private void OnDisplay(Texture2D texture) {
			if (!_image) return;
			_image.texture = texture;
		}
	}
}