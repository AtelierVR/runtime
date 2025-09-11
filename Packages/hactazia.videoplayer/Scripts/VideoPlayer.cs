using System;
using System.Runtime.InteropServices;
using UnityEngine;

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
		public event Action         OnVideoLoaded;
		public event Action         OnVideoEnded;
		public event Action<double> OnTimeChanged;

		private int       playerId = -1;
		private Texture2D videoTexture;
		private double    lastTime = 0.0; // Cache pour éviter les allocations GC
		private byte[]    pixelBuffer;
		private GCHandle  pixelHandle;

		public double Duration
			=> VideoPlayerNative.GetDuration(playerId);

		public double CurrentTime
			=> VideoPlayerNative.GetCurrentTime(playerId);

		public int VideoWidth
			=> VideoPlayerNative.GetVideoWidth(playerId);

		public int VideoHeight
			=> VideoPlayerNative.GetVideoHeight(playerId);

		public double FrameRate
			=> VideoPlayerNative.GetFrameRate(playerId);

		public RenderTexture OutputTexture
			=> renderTexture;

		public int PlayerId
			=> playerId;

		public PlayerState State
			=> playerId >= 0 ? (PlayerState)VideoPlayerNative.GetPlayerState(playerId) : PlayerState.Uninitialized;

		public PlayerError Error
			=> playerId >= 0 ? (PlayerError)VideoPlayerNative.GetPlayerError(playerId) : PlayerError.None;

		public string ErrorMessage
			=> playerId >= 0 ? VideoPlayerNative.GetPlayerErrorMessage(playerId) : "Player not initialized";

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
			if (playerId < 0) return;

			try {
				VideoPlayerNative.UpdatePlayer(playerId);

				var currentState = State;
				if (currentState != PlayerState.Playing) return;

				var currentTime = CurrentTime;
				var duration = Duration;

				// Déclencher l'événement de temps seulement si significativement différent
				if (Math.Abs(currentTime - lastTime) > 0.01) // 10ms de tolérance
				{
					OnTimeChanged?.Invoke(currentTime);
					lastTime = currentTime;
				}

				// Vérifier la fin de la vidéo
				if (currentTime >= duration && duration > 0) {
					if (loop) {
						Seek(0.0);
					} else {
						Stop();
						OnVideoEnded?.Invoke();
					}
				}

				// Récupérer et afficher la frame courante
				UpdateVideoFrame();
			}
			catch (System.Exception ex) {
				Debug.LogError($"VideoPlayer Update error: {ex.Message}");
				// En cas d'erreur, arrêter le player pour éviter d'autres problèmes
				if (playerId >= 0) {
					playerId = -1;
				}
			}
		}

		private void OnDestroy() {
			if (playerId >= 0) {
				VideoPlayerNative.DestroyVideoPlayer(playerId);
				playerId = -1;
			}

			if (pixelHandle.IsAllocated)
				pixelHandle.Free();

			if (videoTexture)
				DestroyImmediate(videoTexture);
		}

		public bool LoadVideo(string url) {
			if (playerId >= 0)
				VideoPlayerNative.DestroyVideoPlayer(playerId);

			playerId = VideoPlayerNative.CreateVideoPlayer();
			if (!VideoPlayerNative.LoadVideo(playerId, url))
				return false;

			OnVideoLoaded?.Invoke();
			Debug.Log($"Video loaded: {VideoWidth}x{VideoHeight}, Duration: {Duration:F2}s, FPS: {FrameRate:F2}");
			return true;
		}

		public void Play() {
			if (playerId < 0 || !IsLoaded) return;
			try {
				VideoPlayerNative.Play(playerId);
			} catch (System.Exception ex) {
				Debug.LogError($"VideoPlayer Play error: {ex.Message}");
			}
		}

		public void Pause() {
			if (playerId < 0 || !IsPlaying) return;
			try {
				VideoPlayerNative.Pause(playerId);
			} catch (System.Exception ex) {
				Debug.LogError($"VideoPlayer Pause error: {ex.Message}");
			}
		}

		public void Resume() {
			if (playerId < 0 || !IsPaused) return;
			try {
				VideoPlayerNative.Resume(playerId);
			} catch (System.Exception ex) {
				Debug.LogError($"VideoPlayer Resume error: {ex.Message}");
			}
		}

		public void Stop() {
			if (playerId < 0 || State == PlayerState.Stopped || State == PlayerState.Uninitialized) return;
			
			try {
				VideoPlayerNative.Stop(playerId);
			}
			catch (System.Exception ex) {
				Debug.LogError($"VideoPlayer Stop error: {ex.Message}");
			}
		}

		public void Seek(double time) {
			if (playerId < 0 || State == PlayerState.Uninitialized || State == PlayerState.Error) return;
			time = Math.Max(0.0, Math.Min(time, Duration));
			VideoPlayerNative.Seek(playerId, time);
		}

		public VideoFrame? GetVideoFrameAtTime(double time) {
			if (playerId < 0 || State == PlayerState.Uninitialized || State == PlayerState.Error) return null;
			var framePtr = VideoPlayerNative.GetVideoFrameAtTime(playerId, time);
			if (framePtr == IntPtr.Zero) return null;

			var frame = Marshal.PtrToStructure<VideoFrame>(framePtr);
			VideoPlayerNative.FreeVideoFrame(framePtr);

			return frame.valid ? frame : null;
		}

		public AudioFrame? GetAudioFrameAtTime(double time) {
			if (playerId < 0 || State == PlayerState.Uninitialized || State == PlayerState.Error) return null;
			var framePtr = VideoPlayerNative.GetAudioFrameAtTime(playerId, time);
			if (framePtr == IntPtr.Zero) return null;

			var frame = Marshal.PtrToStructure<AudioFrame>(framePtr);
			VideoPlayerNative.FreeAudioFrame(framePtr);

			return frame.valid ? frame : null;
		}

		private void UpdateVideoFrame() {
			var frameData = GetVideoFrameAtTime(CurrentTime);
			if (!frameData.HasValue || !renderTexture) return;

			var frame = frameData.Value;
			if (frame.data == IntPtr.Zero) return;

			// Assurer que la RenderTexture est de la bonne taille
			ResizeRenderTexture();
			ResizeTexture();
			ResizeBuffer();

			// Copier les données de la frame native vers notre buffer
			Marshal.Copy(frame.data, pixelBuffer, 0, VideoWidth * VideoHeight * 4);

			// Mettre à jour la texture
			videoTexture.LoadRawTextureData(pixelBuffer);
			videoTexture.Apply();

			Graphics.CopyTexture(videoTexture, renderTexture);
		}

		private void ResizeBuffer() {
			var width        = VideoWidth;
			var height       = VideoHeight;
			var requiredSize = width * height * 4;
			if (pixelBuffer != null && pixelBuffer.Length == requiredSize) return;
			if (pixelHandle.IsAllocated)
				pixelHandle.Free();
			pixelBuffer = new byte[requiredSize];
			pixelHandle = GCHandle.Alloc(pixelBuffer, GCHandleType.Pinned);
		}

		private void ResizeRenderTexture() {
			if (!renderTexture) return;
			var width  = VideoWidth;
			var height = VideoHeight;
			if (renderTexture.width == width && renderTexture.height == height) return;

			renderTexture.Release();
			renderTexture.width  = width;
			renderTexture.height = height;
			renderTexture.Create();
		}

		private void CreateVideoTexture() {
			if (videoTexture)
				DestroyImmediate(videoTexture);
			videoTexture = new Texture2D(VideoWidth, VideoHeight, TextureFormat.RGBA32, false) {
				wrapMode   = TextureWrapMode.Clamp,
				filterMode = FilterMode.Bilinear
			};
		}

		private void ResizeTexture() {
			if (!videoTexture) {
				CreateVideoTexture();
				return;
			}

			var width  = VideoWidth;
			var height = VideoHeight;
			if (videoTexture.width == width && videoTexture.height == height) return;

			CreateVideoTexture();
		}

		// Méthodes publiques pour l'interface Unity
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