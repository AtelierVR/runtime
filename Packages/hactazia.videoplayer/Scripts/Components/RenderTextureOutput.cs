using UnityEngine;
using UnityEngine.UI;

namespace Hactazia.VideoPlayer.Components {
	[DisallowMultipleComponent]
	[RequireComponent(typeof(VideoOutput))]
	public sealed class RenderTextureOutput : MonoBehaviour {
		private bool _subscribed;

		[Header("Sources")]
		[SerializeField]
		private VideoOutput source;

		[Tooltip("RenderTexture that receives the video frames. Leave empty to auto-create one matching the video resolution.")]
		private RenderTexture _renderTexture;

		[Tooltip("Format used when auto-creating the RenderTexture.")]
		[SerializeField]
		private RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGB32;

		[Tooltip("Filter mode applied to the RenderTexture when (re)created.")]
		[SerializeField]
		private FilterMode filterMode = FilterMode.Bilinear;

		[Header("UI")]
		[SerializeField]
		private RawImage targetRawImage;

		[Tooltip("AspectRatioFitter applied to the RawImage. Leave empty to auto-locate one on the same object.")]
		[SerializeField]
		private AspectRatioFitter aspectRatioFitter;

		public RenderTexture GetRenderTexture()
			=> _renderTexture;

		private void Awake() {
			source ??= GetComponent<VideoOutput>();

			if (targetRawImage && !aspectRatioFitter)
				aspectRatioFitter = targetRawImage.GetComponent<AspectRatioFitter>();

			if (aspectRatioFitter)
				aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
		}

		private void UpdateAspect() {
			if (!aspectRatioFitter || !_renderTexture) return;
			aspectRatioFitter.aspectRatio = (float)_renderTexture.width / _renderTexture.height;
		}

		private void OnEnable() {
			Subscribe();

			if (!targetRawImage || !_renderTexture) return;
			targetRawImage.texture = _renderTexture;
			UpdateAspect();
		}

		private void OnDisable()
			=> Unsubscribe();

		private void OnDestroy() {
			Unsubscribe();

			if (!_renderTexture) return;

			if (_renderTexture.IsCreated())
				_renderTexture.Release();

			if (Application.isPlaying)
				Destroy(_renderTexture);
			else DestroyImmediate(_renderTexture);

			_renderTexture = null;
		}

		private void Subscribe() {
			if (!source || _subscribed) return;
			source.OnFrameReady += HandleFrameReady;
			_subscribed         =  true;
		}

		private void Unsubscribe() {
			if (!source || !_subscribed) return;
			source.OnFrameReady -= HandleFrameReady;
			_subscribed         =  false;
		}

		private void HandleFrameReady(Texture2D frame) {
			if (!frame) return;

			EnsureRenderTexture(frame.width, frame.height);
			if (!_renderTexture) return;

			var previous = RenderTexture.active;
			Graphics.Blit(frame, _renderTexture);
			RenderTexture.active = previous;

			if (!targetRawImage) return;

			if (targetRawImage.texture != _renderTexture)
				targetRawImage.texture = _renderTexture;

			UpdateAspect();
		}

		private void EnsureRenderTexture(int width, int height) {
			if (!_renderTexture) {
				_renderTexture = CreateRenderTexture(width, height);
			} else if (_renderTexture.width != width || _renderTexture.height != height) {
				if (_renderTexture.IsCreated())
					_renderTexture.Release();
				_renderTexture.width  = width;
				_renderTexture.height = height;
				_renderTexture.Create();
			}

			if (_renderTexture && !_renderTexture.IsCreated())
				_renderTexture.Create();
		}

		private RenderTexture CreateRenderTexture(int width, int height) {
			width  = Mathf.Max(1, width);
			height = Mathf.Max(1, height);

			var rt = new RenderTexture(width, height, 0, renderTextureFormat) {
				name              = $"VideoRenderTexture_{width}x{height}",
				filterMode        = filterMode,
				enableRandomWrite = false,
				autoGenerateMips  = false,
				useMipMap         = false
			};

			rt.Create();
			return rt;
		}

		public Vector2Int GetResolution()
			=> _renderTexture
				? new Vector2Int(_renderTexture.width, _renderTexture.height)
				: Vector2Int.zero;
	}
}