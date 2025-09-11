using System;
using System.Runtime.InteropServices;
using Nox.CCK.Utils;
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
		public string videoUrl = "";

		[SerializeField]
		private bool playOnStart = false;

		[SerializeField]
		private bool loop = false;

		[Header("Rendering")]
		[SerializeField]
		private RenderTexture renderTexture;

		[Header("Audio")]
		[SerializeField]
		private AudioSource audioSource;

		[SerializeField]
		private bool enableAudio = true;

		[Header("Debug")]
		[SerializeField]
		[HideInInspector]
		public bool showFFmpegDebug = false;


		// Events
		public readonly UnityEvent         OnVideoLoaded = new();
		public readonly UnityEvent         OnVideoEnded  = new();
		public readonly UnityEvent<double> OnTimeChanged = new();

		private IntPtr    _nativePlayer = VideoPlayerNative.InvalidPlayer;
		private Texture2D _videoTexture;
		private double    _lastTime = 0.0;
		private byte[]    _pixelBuffer;
		private GCHandle  _pixelHandle;

		// Audio playback
		private AudioClip _audioClip;
		private float[]   _audioBuffer;
		private int       _audioSampleRate = 44100;
		private int       _audioChannels   = 2;

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

		public bool HasAudio
			=> audioSource && enableAudio;

		public float Volume {
			get => audioSource ? audioSource.volume : 0f;
			set {
				if (audioSource) audioSource.volume = value;
			}
		}

		public bool IsMuted {
			get => audioSource && audioSource.mute;
			set {
				if (audioSource) audioSource.mute = value;
			}
		}

		private void Start() {
			// Get or create AudioSource component if audio is enabled
			if (enableAudio && !audioSource) {
				audioSource             ??= gameObject.GetOrAddComponent<AudioSource>();
				audioSource.playOnAwake =   false;
				audioSource.loop        =   false;
			}

			if (playOnStart && !string.IsNullOrEmpty(videoUrl))
				LoadVideo(videoUrl);
		}

		private void Update() {
			if (!VideoPlayerNative.IsValid(_nativePlayer))
				return;

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
			UpdateAudioFrame();
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
			_lastTime = 0.0;
			// Cache video properties
			_cachedWidth  = VideoWidth;
			_cachedHeight = VideoHeight;

			// Initialize audio if enabled
			if (HasAudio)
				InitializeAudio();

			OnVideoLoaded.Invoke();
			return true;
		}

		public void Play() {
			VideoPlayerNative.Play(_nativePlayer);
			var currentTime = CurrentTime;
			_lastTime = currentTime;

			// Start audio playback
			if (HasAudio && _audioClip) {
				audioSource.clip = _audioClip;
				audioSource.time = (float)currentTime;
				audioSource.Play();
			}
		}

		public void Pause() {
			VideoPlayerNative.Pause(_nativePlayer);
			if (HasAudio && audioSource.isPlaying)
				audioSource.Pause();
		}

		public void Resume() {
			VideoPlayerNative.Resume(_nativePlayer);
			if (HasAudio && !audioSource.isPlaying && _audioClip)
				audioSource.UnPause();
		}

		public void Stop() {
			VideoPlayerNative.Stop(_nativePlayer);
			if (HasAudio && audioSource.isPlaying)
				audioSource.Stop();
		}

		public void Seek(double time) {
			time = Math.Max(0.0, Math.Min(time, Duration));
			VideoPlayerNative.Seek(_nativePlayer, time);
			_lastTime = time;

			// Sync audio playback
			if (HasAudio && _audioClip)
				audioSource.time = (float)time;
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

			// Additional safety checks for frame data integrity
			if (frame.width > 8192 || frame.height > 8192) {
				Logger.LogWarning($"Frame dimensions are suspiciously large: {frame.width}x{frame.height}");
				return;
			}

			// Update cached dimensions if needed
			UpdateCachedDimensions(frame.width, frame.height);

			// Assurer que la RenderTexture est de la bonne taille
			ResizeRenderTexture();
			ResizeTexture();
			ResizeBuffer();

			// Calculate expected buffer size and validate
			var expectedSize = _cachedWidth * _cachedHeight * 4;
			if (_pixelBuffer == null || _pixelBuffer.Length != expectedSize) {
				Logger.LogError($"Pixel buffer size mismatch: expected {expectedSize}, got {_pixelBuffer?.Length ?? 0}");
				return;
			}

			try {
				// Copier les données de la frame native vers notre buffer avec validation
				Marshal.Copy(frame.data, _pixelBuffer, 0, expectedSize);

				// Mettre à jour la texture
				_videoTexture.LoadRawTextureData(_pixelBuffer);
				_videoTexture.Apply();

				Graphics.CopyTexture(_videoTexture, renderTexture);
			} catch (System.Exception ex) {
				Logger.LogError($"Failed to copy frame data: {ex.Message}");
			}
		}

		private void UpdateCachedDimensions(int width = -1, int height = -1) {
			width  = width  > 0 ? width : VideoWidth;
			height = height > 0 ? height : VideoHeight;
			if (_cachedWidth == width && _cachedHeight == height) return;
			_cachedWidth  = width;
			_cachedHeight = height;
			Logger.Log($"Video dimensions changed: {_cachedWidth}x{_cachedHeight}");
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

		private void InitializeAudio() {
			if (!HasAudio) return;

			// Create audio clip for streaming
			var audioBufferSize = _audioSampleRate * _audioChannels * 2; // 2 seconds buffer
			_audioBuffer = new float[audioBufferSize];

			_audioClip = AudioClip.Create("VideoAudio", audioBufferSize, _audioChannels, _audioSampleRate, true, OnAudioRead);
			Logger.Log($"Initialized audio: {_audioSampleRate}Hz, {_audioChannels} channels");
		}

		private void OnAudioRead(float[] data) {
			if (!IsPlaying || !HasAudio) return;

			var currentTime = CurrentTime;
			var audioFrame  = GetAudioFrameAtTime(currentTime);

			if (audioFrame is not { valid: true }) {
				// Fill with silence if no audio data
				Array.Clear(data, 0, data.Length);
				return;
			}

			var frame = audioFrame.Value;
			if (frame.data == IntPtr.Zero || frame.size <= 0) {
				Array.Clear(data, 0, data.Length);
				return;
			}

			// Convert native audio data to Unity's float format
			var audioBytes = new byte[frame.size];
			Marshal.Copy(frame.data, audioBytes, 0, frame.size);

			var sampleCount = Math.Min(data.Length, frame.size / 2); // 16-bit samples
			for (var i = 0; i < sampleCount; i++) {
				if (i * 2 + 1 < audioBytes.Length) {
					// Convert 16-bit signed integer to float (-1.0 to 1.0)
					var sample = (short)(audioBytes[i * 2] | (audioBytes[i * 2 + 1] << 8));
					data[i] = sample / 32768.0f;
				} else data[i] = 0.0f;
			}

			// Fill remaining with silence
			for (var i = sampleCount; i < data.Length; i++)
				data[i] = 0.0f;
		}

		private void UpdateAudioFrame() {
			if (!HasAudio || !IsPlaying) return;

			// Audio is handled by OnAudioRead callback
			// This method can be used for additional audio processing if needed
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

		// ========================================
		// Cache information methods
		// ========================================

		/// <summary>
		/// Gets the current number of cached video frames
		/// </summary>
		public int VideoCacheSize
			=> VideoPlayerNative.GetVideoCacheSize(_nativePlayer);

		/// <summary>
		/// Gets the current number of cached audio frames
		/// </summary>
		public int AudioCacheSize
			=> VideoPlayerNative.GetAudioCacheSize(_nativePlayer);

		/// <summary>
		/// Gets the timestamp of the last cached video frame (-1 if no frames cached)
		/// </summary>
		public double LastVideoCacheTime
			=> VideoPlayerNative.GetLastVideoCacheTime(_nativePlayer);

		/// <summary>
		/// Gets the timestamp of the last cached audio frame (-1 if no frames cached)
		/// </summary>
		public double LastAudioCacheTime
			=> VideoPlayerNative.GetLastAudioCacheTime(_nativePlayer);

		// ========================================
		// Debug information
		// ========================================

		/// <summary>
		/// Gets comprehensive FFmpeg debug information
		/// </summary>
		public string FFMPEGDetails
			=> VideoPlayerNative.GetFFMPEGDetails(_nativePlayer);
	}
}