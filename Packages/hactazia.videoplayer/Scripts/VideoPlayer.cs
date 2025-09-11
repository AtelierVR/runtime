using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Logger = Nox.CCK.Utils.Logger;

namespace Hactazia.VideoPlayer {
	/// <summary>
	/// Wrapper Unity pour le VideoPlayer natif
	/// </summary>
	public class VideoPlayer : MonoBehaviour {
		[Header("Video Settings")]
		[SerializeField]
		private string videoUrl = "";

		[SerializeField]
		private bool playOnStart = false;

		[SerializeField]
		private bool loop = false;

		[Header("Rendering")]
		[SerializeField]
		private RenderTexture renderTexture;


		// Events
		public readonly UnityEvent         OnVideoLoaded = new();
		public readonly UnityEvent         OnVideoEnded  = new();
		public readonly UnityEvent<double> OnTimeChanged = new();

		private IntPtr    _nativePlayer = VideoPlayerNative.InvalidPlayer;
		private Texture2D _videoTexture;
		private double    _lastTime = 0.0;
		private byte[]    _pixelBuffer;
		private GCHandle  _pixelHandle;

		// Cached dimensions to avoid repeated native calls
		private int _cachedWidth  = 0;
		private int _cachedHeight = 0;
		
		public double Duration
			=> VideoPlayerNative.GetDuration(_nativePlayer);

		public double CurrentTime
			=> VideoPlayerNative.GetCurrentTime(_nativePlayer);

		public int VideoWidth
			=> VideoPlayerNative.GetVideoWidth(_nativePlayer);

		public int VideoHeight
			=> VideoPlayerNative.GetVideoHeight(_nativePlayer);

		public double FrameRate
			=> VideoPlayerNative.GetFrameRate(_nativePlayer);

		public RenderTexture OutputTexture
			=> renderTexture;

		public int PlayerId
			=> _nativePlayer.ToInt32();

		public PlayerState State
			=> VideoPlayerNative.GetPlayerState(_nativePlayer);

		public PlayerError Error
			=> VideoPlayerNative.GetPlayerError(_nativePlayer);

		public string ErrorMessage
			=> VideoPlayerNative.GetPlayerErrorMessage(_nativePlayer);

		public bool IsLoaded
			=> State != PlayerState.Uninitialized && State != PlayerState.Error;

		public bool IsPlaying
			=> State == PlayerState.Playing;

		public bool IsPaused
			=> State == PlayerState.Paused;

		public bool IsStopped
			=> State == PlayerState.Stopped;

		public bool HasError
			=> State == PlayerState.Error;

		private void Start() {
			if (playOnStart && !string.IsNullOrEmpty(videoUrl))
				LoadVideo(videoUrl);
		}

		private void Update() {
			if (!VideoPlayerNative.IsValid(_nativePlayer))
				return;

			VideoPlayerNative.UpdatePlayer(_nativePlayer);

			if (State != PlayerState.Playing) return;

			// Déclencher l'événement de temps seulement si significativement différent
			if (Math.Abs(CurrentTime - _lastTime) > 0.01) // 10ms de tolérance
			{
				OnTimeChanged.Invoke(CurrentTime);
				_lastTime = CurrentTime;
			}

			// Vérifier la fin de la vidéo
			if (CurrentTime >= Duration) {
				if (loop) {
					Seek(0.0);
				} else {
					Stop();
					OnVideoEnded.Invoke();
				}
			}

			UpdateVideoFrame();
		}

		private void OnDestroy() {
			VideoPlayerNative.DestroyVideoPlayer(_nativePlayer);
			_nativePlayer = VideoPlayerNative.InvalidPlayer;

			if (_pixelHandle.IsAllocated)
				_pixelHandle.Free();

			if (_videoTexture)
				DestroyImmediate(_videoTexture);
		}

		public bool LoadVideo(string url) {
			VideoPlayerNative.DestroyVideoPlayer(_nativePlayer);
			_nativePlayer = VideoPlayerNative.CreateVideoPlayer();

			if (!VideoPlayerNative.LoadVideo(_nativePlayer, url))
				return false;

			// Reset timing variables for the new video
			_lastTime          = 0.0;
			// Cache video properties
			_cachedWidth  = VideoWidth;
			_cachedHeight = VideoHeight;
			
			OnVideoLoaded.Invoke();
			return true;
		}

		public void Play() {
			VideoPlayerNative.Play(_nativePlayer);
			var currentTime = CurrentTime;
			_lastTime      = currentTime;
		}

		public void Pause()
			=> VideoPlayerNative.Pause(_nativePlayer);

		public void Resume()
			=> VideoPlayerNative.Resume(_nativePlayer);

		public void Stop()
			=> VideoPlayerNative.Stop(_nativePlayer);

		public void Seek(double time) {
			time = Math.Max(0.0, Math.Min(time, Duration));
			VideoPlayerNative.Seek(_nativePlayer, time);
			_lastTime      = time;
		}

		public VideoFrame? GetVideoFrameAtTime(double time) {
			var frame = VideoPlayerNative.GetVideoFrameAtTime(_nativePlayer, time);
			if (!frame.HasValue) return null;
			return frame.Value.valid ? frame : null;
		}

		public AudioFrame? GetAudioFrameAtTime(double time) {
			var frame = VideoPlayerNative.GetAudioFrameAtTime(_nativePlayer, time);
			if (!frame.HasValue) return null;
			return frame.Value.valid ? frame : null;
		}

		private void UpdateVideoFrame() {
			var currentTime = CurrentTime;

			var frameData = GetVideoFrameAtTime(currentTime);
			if (!frameData.HasValue || !renderTexture) return;

			var frame = frameData.Value;
			if (frame.data == IntPtr.Zero || !frame.valid || frame.width <= 0 || frame.height <= 0)
				return;

			// Update cached dimensions if needed
			UpdateCachedDimensions();

			// Assurer que la RenderTexture est de la bonne taille
			ResizeRenderTexture();
			ResizeTexture();
			ResizeBuffer();

			// Copier les données de la frame native vers notre buffer
			Marshal.Copy(frame.data, _pixelBuffer, 0, _cachedWidth * _cachedHeight * 4);

			// Mettre à jour la texture
			_videoTexture.LoadRawTextureData(_pixelBuffer);
			_videoTexture.Apply();

			Graphics.CopyTexture(_videoTexture, renderTexture);
		}

		private void UpdateCachedDimensions() {
			var width  = VideoWidth;
			var height = VideoHeight;
			if (_cachedWidth != width || _cachedHeight != height) {
				_cachedWidth  = width;
				_cachedHeight = height;
				Logger.Log($"Video dimensions changed: {_cachedWidth}x{_cachedHeight}");
			}
		}

		private void ResizeBuffer() {
			var requiredSize = _cachedWidth * _cachedHeight * 4;
			if (_pixelBuffer != null && _pixelBuffer.Length == requiredSize) return;
			if (_pixelHandle.IsAllocated)
				_pixelHandle.Free();
			_pixelBuffer = new byte[requiredSize];
			_pixelHandle = GCHandle.Alloc(_pixelBuffer, GCHandleType.Pinned);
			Logger.Log($"Resized pixel buffer to {_cachedWidth}x{_cachedHeight} ({requiredSize} bytes)");
		}

		private void ResizeRenderTexture() {
			if (!renderTexture) return;
			if (renderTexture.width == _cachedWidth && renderTexture.height == _cachedHeight) return;

			renderTexture.Release();
			renderTexture.width  = _cachedWidth;
			renderTexture.height = _cachedHeight;
			renderTexture.Create();
			Logger.Log($"Resized RenderTexture to {_cachedWidth}x{_cachedHeight}");
		}

		private void CreateVideoTexture() {
			if (_videoTexture)
				DestroyImmediate(_videoTexture);
			_videoTexture = new Texture2D(_cachedWidth, _cachedHeight, TextureFormat.RGBA32, false) {
				wrapMode   = TextureWrapMode.Clamp,
				filterMode = FilterMode.Bilinear
			};
			Logger.Log($"Created video texture {_cachedWidth}x{_cachedHeight}");
		}

		private void ResizeTexture() {
			if (!_videoTexture) {
				CreateVideoTexture();
				return;
			}

			if (_videoTexture.width == _cachedWidth && _videoTexture.height == _cachedHeight) return;

			CreateVideoTexture();
			Logger.Log($"Resized video texture to {_cachedWidth}x{_cachedHeight}");
		}

		[ContextMenu("Play Video")]
		public void PlayVideo()
			=> Play();

		[ContextMenu("Pause Video")]
		public void PauseVideo()
			=> Pause();

		[ContextMenu("Stop Video")]
		public void StopVideo()
			=> Stop();
	}
}