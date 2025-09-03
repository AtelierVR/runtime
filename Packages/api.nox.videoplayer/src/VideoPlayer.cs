using UnityEngine;
using UnityEngine.Video;
using System.Diagnostics;
using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Xml;
using api.nox.videoplayer;
using Newtonsoft.Json.Linq;
using Formatting = Newtonsoft.Json.Formatting;
using Logger = Nox.CCK.Utils.Logger;

public class YouTubeVideoPlayer : MonoBehaviour {
	[Header("YouTube Video Settings")]
	[SerializeField]
	private string youtubeUrl = "";

	[SerializeField]
	private VideoRenderMode renderMode = VideoRenderMode.RenderTexture;

	[SerializeField]
	private RenderTexture targetRenderTexture;

	[SerializeField]
	private Renderer targetRenderer;

	[SerializeField]
	private AudioSource audioSource;

	[Header("Quality Settings")]
	[SerializeField]
	private YouTubeQuality preferredQuality = YouTubeQuality.Best;

	[Header("Controls")]
	[SerializeField]
	private bool autoPlay = true;

	[SerializeField]
	private bool loop;

	[Header("Debug")]
	[SerializeField]
	private bool enableDebugLogs = true;

	private VideoPlayer             _videoPlayer;
	private bool                    _isInitialized;
	private CancellationTokenSource _cancellationTokenSource;
	private string                  _currentYouTubeUrl;
	private bool                    _isRefreshing;

	[Header("URL Refresh Settings")]
	[SerializeField]
	private float urlRefreshIntervalMinutes = 30f; // Rafraîchir l'URL toutes les 30 minutes par défaut

	public enum YouTubeQuality {
		Best,
		Worst,
		Hd720P,
		Hd1080P,
		Hd1440P,
		Hd2160P
	}

	public event Action         OnVideoReady;
	public event Action         OnVideoStarted;
	public event Action         OnVideoFinished;
	public event Action<string> OnError;

	void Start() {
		_cancellationTokenSource = new CancellationTokenSource();
		InitializeVideoPlayer();

		if (!string.IsNullOrEmpty(youtubeUrl) && autoPlay) {
			LoadAndPlayVideoAsync(youtubeUrl, _cancellationTokenSource.Token).Forget();
		}

		OnError += (message) => {
			if (enableDebugLogs)
				UnityEngine.Debug.LogError($"YouTubeVideoPlayer Error: {message}");
		};

		OnVideoReady += () => {
			if (enableDebugLogs)
				UnityEngine.Debug.Log("YouTubeVideoPlayer: Video is ready to play.");
		};

		OnVideoStarted += () => {
			if (enableDebugLogs)
				UnityEngine.Debug.Log("YouTubeVideoPlayer: Video playback started.");
		};

		OnVideoFinished += () => {
			if (enableDebugLogs)
				UnityEngine.Debug.Log("YouTubeVideoPlayer: Video playback finished.");
		};
	}

	private void InitializeVideoPlayer() {
		_videoPlayer = GetComponent<VideoPlayer>();
		if (_videoPlayer == null) {
			if (enableDebugLogs)
				UnityEngine.Debug.LogError("Aucun composant VideoPlayer trouvé sur ce GameObject. Veuillez ajouter un VideoPlayer avant d'utiliser YouTubeVideoPlayer.");
			return;
		}

		_videoPlayer.renderMode = renderMode;
		_videoPlayer.isLooping  = loop;

		if (renderMode == VideoRenderMode.RenderTexture && targetRenderTexture != null) {
			_videoPlayer.targetTexture = targetRenderTexture;
		} else if (renderMode == VideoRenderMode.MaterialOverride && targetRenderer != null) {
			_videoPlayer.targetMaterialRenderer = targetRenderer;
		}

		if (audioSource != null) {
			_videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
			_videoPlayer.SetTargetAudioSource(0, audioSource);
		}

		// Événements du VideoPlayer
		_videoPlayer.prepareCompleted += OnVideoPrepared;
		_videoPlayer.started          += OnVideoPlayerStarted;
		_videoPlayer.loopPointReached += OnVideoPlayerFinished;
		_videoPlayer.errorReceived    += OnVideoPlayerError;

		_isInitialized = true;

		if (enableDebugLogs)
			UnityEngine.Debug.Log("YouTube VideoPlayer initialisé");
	}

	public void PlayYouTubeVideo(string url) {
		if (!_isInitialized) {
			OnError?.Invoke("VideoPlayer non initialisé");
			return;
		}

		youtubeUrl = url;

		// Annuler le chargement précédent s'il existe
		_cancellationTokenSource?.Cancel();
		_cancellationTokenSource = new CancellationTokenSource();

		LoadAndPlayVideoAsync(url, _cancellationTokenSource.Token).Forget();
	}

	private async UniTask LoadAndPlayVideoAsync(string url, CancellationToken cancellationToken = default) {
		if (!await YtDl.WaitReady(cancellationToken: cancellationToken)) {
			OnError?.Invoke("yt-dlp non disponible");
			return;
		}

		if (enableDebugLogs)
			UnityEngine.Debug.Log($"Chargement de la vidéo YouTube: {url}");

		try {
			// Arrêter la vidéo actuelle si elle joue
			if (_videoPlayer.isPlaying) {
				_videoPlayer.Stop();
			}

			// Stocker l'URL YouTube originale pour le rafraîchissement
			_currentYouTubeUrl = url;

			// Exécuter yt-dlp de manière asynchrone
			var stream = await Fetch(url, cancellationToken);
			Logger.LogDebug($"YouTube video: {url}");

			if (cancellationToken.IsCancellationRequested)
				return;

			if (stream == null) {
				OnError?.Invoke("Impossible d'obtenir les informations vidéo");
				return;
			}

			if (enableDebugLogs)
				UnityEngine.Debug.Log($"Informations vidéo obtenues: {stream.ToString(Formatting.Indented)}");

			if (stream["formats"] is not JArray formats || formats.Count == 0) {
				OnError?.Invoke("Aucun format vidéo disponible");
				return;
			}

			string videoUrl = null;

			// Extraire l'URL de streaming en fonction de la qualité préférée
			var videoFormats = formats
				.Where(f => f["height"].Type != JTokenType.Null)
				.OrderBy(f => f["height"] != null ? (int)f["height"] : 0)
				.ToArray();

			if (videoFormats.Length == 0) {
				if (enableDebugLogs)
					UnityEngine.Debug.LogWarning("Aucun format avec audio trouvé, utilisation des formats disponibles");
				videoFormats = formats.ToArray();
			}

			Logger.LogDebug($"Best Size: {videoFormats.LastOrDefault()?["height"]}, Worst Size: {videoFormats.FirstOrDefault()?["height"]}");

			switch (preferredQuality) {
				case YouTubeQuality.Best:
					videoUrl = videoFormats.LastOrDefault()?["url"]?.ToString();
					break;
				case YouTubeQuality.Worst:
					videoUrl = videoFormats.FirstOrDefault()?["url"]?.ToString();
					break;
				case YouTubeQuality.Hd720P:
					videoUrl = videoFormats
						.Where(f => f["height"] != null && (int)f["height"] <= 720)
						.Select(f => f["url"]?.ToString())
						.LastOrDefault();
					break;
				case YouTubeQuality.Hd1080P:
					videoUrl = videoFormats
						.Where(f => f["height"] != null && (int)f["height"] <= 1080)
						.Select(f => f["url"]?.ToString())
						.LastOrDefault();
					break;
				case YouTubeQuality.Hd1440P:
					videoUrl = videoFormats
						.Where(f => f["height"] != null && (int)f["height"] <= 1440)
						.Select(f => f["url"]?.ToString())
						.LastOrDefault();
					break;
				case YouTubeQuality.Hd2160P:
					videoUrl = videoFormats
						.Where(f => f["height"] != null && (int)f["height"] >= 2160)
						.Select(f => f["url"]?.ToString())
						.FirstOrDefault();
					break;
			}


			// Si aucun format avec la qualité demandée n'est trouvé, utiliser le meilleur format avec audio disponible
			if (string.IsNullOrEmpty(videoUrl) && videoFormats.Length > 0) {
				if (enableDebugLogs)
					UnityEngine.Debug.LogWarning($"Aucun format {preferredQuality} trouvé, utilisation du meilleur format disponible");
				videoUrl = videoFormats.LastOrDefault()?["url"]?.ToString();
			}

			string audioUrl = null;

			var audioFormats = formats
				.Where(f => f["acodec"] != null && f["acodec"].ToString() != "none")
				.OrderBy(f => f["abr"] != null && f["abr"].Type != JTokenType.Null ? (int)f["abr"] : 0)
				.ToArray();

			if (audioFormats.Length > 0)
				audioUrl = audioFormats.LastOrDefault()?["url"]?.ToString();

			if (string.IsNullOrEmpty(videoUrl)) {
				OnError?.Invoke("Aucun format vidéo trouvé");
				return;
			}

			if (enableDebugLogs) {
				UnityEngine.Debug.Log($"URL Vidéo: {videoUrl}");
				if (!string.IsNullOrEmpty(audioUrl))
					UnityEngine.Debug.Log($"URL Audio: {audioUrl}");
			}

			// Charger et jouer la vidéo
			_videoPlayer.url = videoUrl;
			_videoPlayer.Prepare();
		} catch (OperationCanceledException) {
			if (enableDebugLogs)
				UnityEngine.Debug.Log("Chargement de la vidéo annulé");
		} catch (Exception ex) {
			Logger.LogException(ex);
			OnError?.Invoke($"Erreur lors du chargement: {ex.Message}");
		}
	}

	private async UniTask<JObject> Fetch(string videoUrl, CancellationToken cancellationToken = default) {
		try {
			return await YtDl.Extract(videoUrl, cancellationToken: cancellationToken);
		} catch (Exception e) {
			OnError?.Invoke($"Erreur yt-dlp: {e.Message}");
			return null;
		}
	}

	// Méthodes de contrôle public
	public void Play() {
		if (_videoPlayer != null && _videoPlayer.isPrepared) {
			_videoPlayer.Play();
		}
	}

	public void Pause() {
		if (_videoPlayer != null && _videoPlayer.isPlaying) {
			_videoPlayer.Pause();
		}
	}

	public void Stop() {
		if (_videoPlayer != null) {
			_videoPlayer.Stop();
		}

		// Annuler le chargement en cours
		_cancellationTokenSource?.Cancel();
	}

	public void SetVolume(float volume) {
		if (audioSource != null) {
			audioSource.volume = Mathf.Clamp01(volume);
		}
	}

	public void Seek(float time) {
		if (_videoPlayer != null && _videoPlayer.isPrepared) {
			_videoPlayer.time = time;
		}
	}

	public float GetCurrentTime() {
		return _videoPlayer != null ? (float)_videoPlayer.time : 0f;
	}

	public float GetDuration() {
		return _videoPlayer != null ? (float)_videoPlayer.length : 0f;
	}

	public bool IsPlaying() {
		return _videoPlayer != null && _videoPlayer.isPlaying;
	}

	[ContextMenu("Play YouTube Video")]
	private void TestPlayYouTubeVideo() {
		if (string.IsNullOrEmpty(youtubeUrl)) {
			UnityEngine.Debug.LogWarning("Aucune URL YouTube configurée. Veuillez entrer une URL dans le champ 'Youtube Url' avant de tester.");
			return;
		}

		PlayYouTubeVideo(youtubeUrl);
	}

	// Événements du VideoPlayer Unity
	private void OnVideoPrepared(VideoPlayer vp) {
		if (enableDebugLogs)
			UnityEngine.Debug.Log("Vidéo préparée et prête à jouer");

		OnVideoReady?.Invoke();

		if (autoPlay) {
			vp.Play();
		}
	}

	private void OnVideoPlayerStarted(VideoPlayer vp) {
		if (enableDebugLogs)
			UnityEngine.Debug.Log("Lecture vidéo démarrée");

		OnVideoStarted?.Invoke();
	}

	private void OnVideoPlayerFinished(VideoPlayer vp) {
		if (enableDebugLogs)
			UnityEngine.Debug.Log("Lecture vidéo terminée");

		OnVideoFinished?.Invoke();
	}

	private void OnVideoPlayerError(VideoPlayer vp, string message) {
		UnityEngine.Debug.LogError($"Erreur VideoPlayer: {message}");
		OnError?.Invoke(message);
	}

	void OnDestroy() {
		// Annuler toutes les opérations en cours
		_cancellationTokenSource?.Cancel();
		_cancellationTokenSource?.Dispose();

		if (_videoPlayer != null) {
			_videoPlayer.prepareCompleted -= OnVideoPrepared;
			_videoPlayer.started          -= OnVideoPlayerStarted;
			_videoPlayer.loopPointReached -= OnVideoPlayerFinished;
			_videoPlayer.errorReceived    -= OnVideoPlayerError;
		}
	}
}