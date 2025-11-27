using Hactazia.FFPlay;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Hactazia.FFPlay {
	[RequireComponent(typeof(AspectRatioFitter))]
	public class VideoForAspectRatioFitter : MonoBehaviour {
		public VideoWorker worker;

		private AspectRatioFitter _fitter;

		private void Start() {
			worker ??= GetComponentInParent<VideoWorker>();

			if (!worker) {
				Debug.LogWarning($"No {nameof(VideoWorker)} found.");
				return;
			}

			_fitter = GetComponent<AspectRatioFitter>();
			if (_fitter.aspectMode == AspectRatioFitter.AspectMode.None)
				_fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

			worker.OnResize.AddListener(OnResize);
		}

		private void OnDestroy()
			=> worker?.OnResize.RemoveListener(OnResize);

		private void OnResize(Vector2Int size) {
			if (size.magnitude <= 0) return;
			var aspectRatio = (float)size.x / size.y;
			_fitter.aspectRatio = aspectRatio;
			Debug.Log($"[DisplayAspectRatioFitter] Updated aspect ratio to {aspectRatio:F3} ({size.x}x{size.y})");
		}
	}
}