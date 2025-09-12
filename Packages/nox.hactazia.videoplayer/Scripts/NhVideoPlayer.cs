using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Hactazia.VideoPlayer;
using Nox.CCK.VideoPlayer;
using Nox.VideoPlayer;
using UnityEngine;
using UnityEngine.Events;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Hactazia.VideoPlayer {
	[DisplayName("Nox Hactazia's Video Player")]
	public class NhVideoPlayer : MonoBehaviour, IVideoPlayer, IVideoPlayerResolver {
		private IntPtr        _nativePlayer;
		public  RenderTexture render;
		public  string        videoUrl = "";
		public  bool          playOnStart;

		private void Start() {
			if (!playOnStart || string.IsNullOrEmpty(videoUrl)) return;
			Play(videoUrl);
		}


		private void Update() {
			if (_nativePlayer == IntPtr.Zero || !IsPlaying()) return;
			UpdateVideoFrame();
		}

		#region Video Frame Handling

		private int       _cachedWidth  = 1920;
		private int       _cachedHeight = 1080;
		private Texture2D _videoTexture;
		private byte[]    _pixelBuffer;
		private GCHandle  _pixelHandle;

		private void UpdateVideoFrame() {
			// Early validation
			if (_nativePlayer == IntPtr.Zero) {
				Logger.LogError("Native player is invalid");
				return;
			}

			var frameData = VideoPlayerNative.GetVideoFrame(_nativePlayer);
			if (!frameData.HasValue || !render) return;
			var frame = frameData.Value;

			// Enhanced validation
			if (frame.data  == IntPtr.Zero || !frame.valid) return;
			if (frame.width <= 0           || frame.height <= 0) return;
			if (frame.width > 8192         || frame.height > 8192) return;

			// Check for reasonable frame size to prevent huge memory allocations
			var expectedSize = frame.width * frame.height * 4;
			if (expectedSize > 268435456) {
				// 256MB limit
				Logger.LogError($"Frame size too large: {expectedSize} bytes ({frame.width}x{frame.height})");
				return;
			}

			UpdateCachedDimensions(frame.width, frame.height);
			ResizeRenderTexture();
			ResizeTexture();
			ResizeBuffer();

			var bufferSize = _cachedWidth * _cachedHeight * 4;
			if (_pixelBuffer == null || _pixelBuffer.Length != bufferSize) {
				Logger.LogWarning($"Buffer size mismatch: expected {bufferSize}, got {_pixelBuffer?.Length ?? 0}");
				return;
			}

			// Additional safety checks
			if (_videoTexture == null) {
				Logger.LogWarning("Video texture is null, skipping frame update");
				return;
			}

			try {
				// Validate that we have a valid texture and buffer before copying
				if (_pixelBuffer.Length == bufferSize && _cachedWidth > 0 && _cachedHeight > 0) {
					Marshal.Copy(frame.data, _pixelBuffer, 0, bufferSize);
					_videoTexture.LoadRawTextureData(_pixelBuffer);
					_videoTexture.Apply();
					Graphics.CopyTexture(_videoTexture, render);
				}
			} catch (Exception ex) {
				Logger.LogError($"Error updating video frame: {ex.Message}");
				// Optionally reset buffers on error to prevent further issues
				CleanupBuffers();
			}
		}

		private void CreateVideoTexture() {
			if (_videoTexture)
				DestroyImmediate(_videoTexture);
			_videoTexture = new Texture2D(
				_cachedWidth,
				_cachedHeight,
				TextureFormat.RGBA32,
				false
			) {
				wrapMode   = TextureWrapMode.Clamp,
				filterMode = FilterMode.Bilinear
			};
		}

		private void ResizeTexture() {
			if (!_videoTexture)
				CreateVideoTexture();
			else if (_videoTexture.width != _cachedWidth || _videoTexture.height != _cachedHeight)
				CreateVideoTexture();
		}

		private void UpdateCachedDimensions(int width, int height) {
			if (_cachedWidth == width && _cachedHeight == height) return;
			_cachedWidth  = width;
			_cachedHeight = height;
		}

		private void ResizeBuffer() {
			var requiredSize = _cachedWidth * _cachedHeight * 4;
			if (_pixelBuffer != null && _pixelBuffer.Length == requiredSize) return;
			if (_pixelHandle.IsAllocated) _pixelHandle.Free();
			_pixelBuffer = new byte[requiredSize];
			_pixelHandle = GCHandle.Alloc(_pixelBuffer, GCHandleType.Pinned);
		}

		private void CleanupBuffers() {
			if (_pixelHandle.IsAllocated) {
				_pixelHandle.Free();
			}

			_pixelBuffer = null;

			if (_videoTexture) {
				DestroyImmediate(_videoTexture);
				_videoTexture = null;
			}
		}

		private void ResizeRenderTexture() {
			if (!render) return;
			if (render.width == _cachedWidth && render.height == _cachedHeight) return;
			render.Release();
			render.width  = _cachedWidth;
			render.height = _cachedHeight;
			render.Create();
		}

		#endregion Video Frame Handling


		#region Events

		public UnityEvent<IVideoPlayer>            onReady             = new(); // Événement pour la préparation terminée
		public UnityEvent<IVideoPlayer>            onStart             = new(); // Événement pour le début de la lecture
		public UnityEvent<IVideoPlayer>            onEnd               = new(); // Événement pour la fin de la lecture
		public UnityEvent<IVideoPlayer, Exception> onError             = new(); // Événement pour les erreurs
		public UnityEvent<IVideoPlayer>            onConnectionLost    = new(); // Événement pour perte de connexion
		public UnityEvent<IVideoPlayer>            onReconnecting      = new(); // Événement pour tentative de reconnexion
		public UnityEvent<IVideoPlayer>            onReconnected       = new(); // Événement pour reconnexion réussie
		public UnityEvent<IVideoPlayer>            onPlay              = new(); // Événement pour la lecture
		public UnityEvent<IVideoPlayer>            onPause             = new(); // Événement pour la pause
		public UnityEvent<IVideoPlayer>            onResume            = new(); // Événement pour la reprise
		public UnityEvent<IVideoPlayer, double>    onProgress          = new(); // Événement pour la progression (0.0 à 1.0)
		public UnityEvent<IVideoPlayer, double>    onSeek              = new(); // Événement pour le seek (temps en secondes)
		public UnityEvent<IVideoPlayer, float>     onVolumeChanged     = new(); // Événement pour le changement de volume (0.0 à 1.0)
		public UnityEvent<IVideoPlayer, bool>      onPlayStatusChanged = new(); // Événement pour le changement de statut de lecture

		public UnityEvent<IVideoPlayer, IFetchOptions>            onResolving = new(); // Événement pour le début de la résolution
		public UnityEvent<IVideoPlayer, IFetchOptions, IResult[]> onResolved  = new(); // Événement pour la résolution terminée

		public UnityEvent<IVideoPlayer> OnReadyEvent()
			=> onReady;

		public UnityEvent<IVideoPlayer> OnConnectionLost()
			=> onConnectionLost;

		public UnityEvent<IVideoPlayer> OnReconnecting()
			=> onReconnecting;

		public UnityEvent<IVideoPlayer> OnReconnected()
			=> onReconnected;

		public UnityEvent<IVideoPlayer> OnPlayEvent()
			=> onPlay;

		public UnityEvent<IVideoPlayer> OnPauseEvent()
			=> onPause;

		public UnityEvent<IVideoPlayer> OnResumeEvent()
			=> onResume;

		public UnityEvent<IVideoPlayer, double> OnProgressEvent()
			=> onProgress;

		public UnityEvent<IVideoPlayer, double> OnSeekEvent()
			=> onSeek;

		public UnityEvent<IVideoPlayer, float> OnVolumeChangedEvent()
			=> onVolumeChanged;

		public UnityEvent<IVideoPlayer, bool> OnPlayStatusChangedEvent()
			=> onPlayStatusChanged;

		public UnityEvent<IVideoPlayer, Exception> OnErrorEvent()
			=> onError;

		public UnityEvent<IVideoPlayer> OnStartEvent()
			=> onStart;

		public UnityEvent<IVideoPlayer> OnEndEvent()
			=> onEnd;

		public UnityEvent<IVideoPlayer, IFetchOptions> OnResolvingEvent()
			=> onResolving;

		public UnityEvent<IVideoPlayer, IFetchOptions, IResult[]> OnResolvedEvent()
			=> onResolved;

		#endregion Events

		private void Awake() {
			_nativePlayer = VideoPlayerNative.CreateVideoPlayer();
			if (_nativePlayer == IntPtr.Zero) {
				Logger.LogError("Failed to create native video player");
			}
		}

		private void OnDestroy() {
			CleanupBuffers();
			if (_nativePlayer != IntPtr.Zero) {
				VideoPlayerNative.DestroyVideoPlayer(_nativePlayer);
				_nativePlayer = IntPtr.Zero;
			}
		}

		public bool IsPlaying()
			=> VideoPlayerNative.GetPlayerState(_nativePlayer) == PlayerState.Playing;

		#region Plays

		public void Play(string query)
			=> Play(query, IsLooping());

		public void Play(string query, bool loop) {
			Logger.LogDebug($"Play called with query: {query}, loop: {loop}");
			Resolve(new VideoFetchOptions { Query = query });
			SetLooping(loop);
		}

		private void PlayUrl(string url, bool loop) {
			if (string.IsNullOrEmpty(url)) {
				Logger.LogError("PlayUrl called with null or empty URL");
				onError.Invoke(this, new ArgumentNullException(nameof(url)));
				return;
			}

			if (!VideoPlayerNative.LoadVideo(_nativePlayer, url)) {
				Logger.LogError($"Failed to load video URL: {url}");
				onError.Invoke(this, new Exception(VideoPlayerNative.GetPlayerErrorMessage(_nativePlayer)));
				return;
			}

			Logger.LogDebug($"Playing URL: {url} with loop: {loop}");
			VideoPlayerNative.Play(_nativePlayer);
			onPlay.Invoke(this);
		}

		private void Play((IFormat, IFormat) tuple, bool loop) {
			if (tuple.Item1 is IAudioVideo av) {
				Play(av);
				return;
			}

			if (tuple is { Item1: IVideo v, Item2: IAudio a }) {
				Play(v, a);
				return;
			}

			switch (tuple.Item1) {
				case IVideo v2:
					Play(v2);
					break;
				case IAudio a2:
					Play(a2);
					break;
				default:
					Logger.LogError("Tuple contains unsupported format types");
					break;
			}
		}

		private void Play(IAudioVideo audioVideo) {
			if (audioVideo == null) {
				Logger.LogError("AudioVideo format is null");
				onError.Invoke(this, new ArgumentNullException(nameof(audioVideo)));
				return;
			}

			var url = audioVideo.GetUrl();
			if (string.IsNullOrEmpty(url)) {
				Logger.LogError("AudioVideo URL is null or empty");
				onError.Invoke(this, new Exception("AudioVideo URL is null or empty"));
				return;
			}

			Logger.LogDebug($"Playing AudioVideo format: {url} (Resolution: {audioVideo.GetResolution()}, Audio Channels: {audioVideo.GetAudioChannels()})");

			PlayUrl(url, IsLooping());
		}

		private void Play(IVideo v, IAudio a) {
			if (v == null || a == null) {
				Logger.LogError($"Video or Audio format is null - Video: {v != null}, Audio: {a != null}");
				onError.Invoke(this, new ArgumentNullException(v == null ? nameof(v) : nameof(a)));
				return;
			}

			var vUrl = v.GetUrl();
			var aUrl = a.GetUrl();

			if (string.IsNullOrEmpty(vUrl) || string.IsNullOrEmpty(aUrl)) {
				Logger.LogError($"Video or Audio URL is null or empty - Video URL: {!string.IsNullOrEmpty(vUrl)}, Audio URL: {!string.IsNullOrEmpty(aUrl)}");
				onError.Invoke(this, new Exception("Video or Audio URL is null or empty"));
				return;
			}

			Logger.LogDebug($"Playing separate Video and Audio streams - Video: {vUrl}, Audio: {aUrl}");
			Logger.LogDebug($"Video Resolution: {v.GetResolution()}, Audio Channels: {a.GetAudioChannels()}");

			// Pour des streams séparés, Unity VideoPlayer ne supporte pas nativement deux URLs
			// On doit utiliser la vidéo comme source principale et gérer l'audio séparément
			Logger.LogWarning("Separate video/audio streams detected. Using video stream only - audio stream will be ignored.");

			Play(v);
		}

		private void Play(IVideo v) {
			if (v == null) {
				Logger.LogError("Video format is null");
				onError.Invoke(this, new ArgumentNullException(nameof(v)));
				return;
			}

			var url = v.GetUrl();
			if (string.IsNullOrEmpty(url)) {
				Logger.LogError("Video URL is null or empty");
				onError.Invoke(this, new Exception("Video URL is null or empty"));
				return;
			}

			Logger.LogDebug($"Playing Video format: {url} (Resolution: {v.GetResolution()}, Framerate: {v.GetFramerate()})");

			PlayUrl(url, IsLooping());
		}

		private void Play(IAudio a) {
			if (a == null) {
				Logger.LogError("Audio format is null");
				onError.Invoke(this, new ArgumentNullException(nameof(a)));
				return;
			}

			var url = a.GetUrl();
			if (string.IsNullOrEmpty(url)) {
				Logger.LogError("Audio URL is null or empty");
				onError.Invoke(this, new Exception("Audio URL is null or empty"));
				return;
			}

			Logger.LogDebug($"Playing Audio format: {url} (Channels: {a.GetAudioChannels()}, Bitrate: {a.GetAudioBitrate()})");

			PlayUrl(url, IsLooping());
		}

		#endregion Plays

		#region Resolve

		private IFetchOptions _currentFetchOptions;
		private IResolve      _currentPlaying;

		private void Resolve(IFetchOptions fetchOptions) {
			if (_currentFetchOptions != null)
				_currentFetchOptions.GetCancellation().Cancel();
			_currentFetchOptions = fetchOptions;
			Logger.LogDebug($"Resolving {_currentFetchOptions?.ToString() ?? "null"}");
			onResolving.Invoke(this, fetchOptions);
		}

		public void OnResolve(IFetchOptions initial, IResult[] results) {
			if (_currentFetchOptions != initial) return; // Ignore if not the current fetch options

			foreach (var result in results) {
				if (result == null || !result.IsError()) continue;
				Logger.LogWarning($"Error during fetch for {_currentFetchOptions?.ToString() ?? "null"}: {result.GetError()}");
			}

			var resolves = results.SelectMany(e => e.GetData()).ToArray();

			if (resolves.Length == 0) {
				Logger.LogWarning($"No data found for {_currentFetchOptions?.ToString() ?? "null"}");
				onError.Invoke(this, new Exception("No data found"));
				return;
			}

			var first = resolves.FirstOrDefault();

			if (first == null) {
				Logger.LogWarning($"No results found for {_currentFetchOptions?.ToString() ?? "null"}");
				onError.Invoke(this, new Exception("No results found"));
				return;
			}

			var tuple = first.FindQuality();

			if (tuple.Item1 == null) {
				Logger.LogWarning($"No compatible stream found for {_currentFetchOptions?.ToString() ?? "null"}");
				onError.Invoke(this, new Exception("No compatible stream found"));
				return;
			}

			onResolved.Invoke(this, initial, results);
			_currentPlaying = first;

			Play(tuple, IsLooping());
		}

		#endregion Resolve

		public void Stop()
			=> VideoPlayerNative.Stop(_nativePlayer);

		public void Pause()
			=> VideoPlayerNative.Pause(_nativePlayer);

		public void Resume()
			=> VideoPlayerNative.Resume(_nativePlayer);

		public float GetVolume()
			=> 0;

		public void SetVolume(float volume) { }

		public void SetSeek(double time)
			=> VideoPlayerNative.Seek(_nativePlayer, time);

		public double GetTime()
			=> VideoPlayerNative.GetCurrentTime(_nativePlayer);

		public double GetDuration()
			=> VideoPlayerNative.GetDuration(_nativePlayer);

		public double GetProgress()
			=> GetDuration() > 0 ? GetTime() / GetDuration() : 0;


		public bool IsLooping()
			=> false;

		public void SetLooping(bool loop) { }

		public RenderTexture GetRender()
			=> render;
	}
}