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

		private IntPtr    nativePlayer = IntPtr.Zero;
		private Texture2D videoTexture;
		private double    lastTime = 0.0; // Cache pour éviter les allocations GC
		private byte[]    pixelBuffer;
		private GCHandle  pixelHandle;

		public double Duration
			=> VideoPlayerNative.GetDuration(nativePlayer);

		public double CurrentTime
			=> VideoPlayerNative.GetCurrentTime(nativePlayer);

		public int VideoWidth
			=> VideoPlayerNative.GetVideoWidth(nativePlayer);

		public int VideoHeight
			=> VideoPlayerNative.GetVideoHeight(nativePlayer);

		public double FrameRate
			=> VideoPlayerNative.GetFrameRate(nativePlayer);

		public RenderTexture OutputTexture
			=> renderTexture;

		public int PlayerId
			=> nativePlayer.ToInt32();

		public PlayerState State
			=> (PlayerState)VideoPlayerNative.GetPlayerState(nativePlayer);

		public PlayerError Error
			=> (PlayerError)VideoPlayerNative.GetPlayerError(nativePlayer);

		public string ErrorMessage
			=> VideoPlayerNative.GetPlayerErrorMessage(nativePlayer);

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
			if (nativePlayer == IntPtr.Zero)
				return;

			VideoPlayerNative.UpdatePlayer(nativePlayer);

			if (State != PlayerState.Playing) return;

			// Déclencher l'événement de temps seulement si significativement différent
			if (Math.Abs(CurrentTime - lastTime) > 0.01) // 10ms de tolérance
			{
				OnTimeChanged?.Invoke(CurrentTime);
				lastTime = CurrentTime;
			}

			// Vérifier la fin de la vidéo
			if (CurrentTime >= Duration) {
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

		private void OnDestroy() {
			VideoPlayerNative.DestroyVideoPlayer(nativePlayer);
			nativePlayer = IntPtr.Zero;

			if (pixelHandle.IsAllocated)
				pixelHandle.Free();

			if (videoTexture)
				DestroyImmediate(videoTexture);
		}

		public bool LoadVideo(string url) {
			VideoPlayerNative.DestroyVideoPlayer(nativePlayer);
			
			nativePlayer = VideoPlayerNative.CreateVideoPlayer();
			if (!VideoPlayerNative.LoadVideo(nativePlayer, url))
				return false;

			OnVideoLoaded?.Invoke();
			Debug.Log($"Video loaded: {VideoWidth}x{VideoHeight}, Duration: {Duration:F2}s, FPS: {FrameRate:F2}");
			return true;
		}

		public void Play()
			=> VideoPlayerNative.Play(nativePlayer);

		public void Pause()
			=> VideoPlayerNative.Pause(nativePlayer);


		public void Resume()
			=> VideoPlayerNative.Resume(nativePlayer);

		public void Stop()
			=> VideoPlayerNative.Stop(nativePlayer);

		public void Seek(double time) {
			if (nativePlayer == IntPtr.Zero || State == PlayerState.Uninitialized || State == PlayerState.Error) return;
			time = Math.Max(0.0, Math.Min(time, Duration));
			VideoPlayerNative.Seek(nativePlayer, time);
		}

		public VideoFrame? GetVideoFrameAtTime(double time) {
			if (nativePlayer == IntPtr.Zero || State == PlayerState.Uninitialized || State == PlayerState.Error) return null;
			var framePtr = VideoPlayerNative.GetVideoFrameAtTime(nativePlayer, time);
			if (framePtr == IntPtr.Zero) return null;

			var frame = Marshal.PtrToStructure<VideoFrame>(framePtr);
			VideoPlayerNative.FreeVideoFrame(framePtr);

			return frame.valid ? frame : null;
		}

		public AudioFrame? GetAudioFrameAtTime(double time) {
			if (nativePlayer == IntPtr.Zero || State == PlayerState.Uninitialized || State == PlayerState.Error) return null;
			var framePtr = VideoPlayerNative.GetAudioFrameAtTime(nativePlayer, time);
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