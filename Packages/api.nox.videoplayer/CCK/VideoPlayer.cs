using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.VideoPlayer;
using UnityEngine;
using UnityEngine.Events;
using Logger = Nox.CCK.Utils.Logger;
using UnityVideoPlayer = UnityEngine.Video.VideoPlayer;

namespace Nox.CCK.VideoPlayer {
	public class VideoPlayer : MonoBehaviour, IVideoPlayer, IVideoPlayerEvents {
		#region Events

		public UnityEvent            onReady             = new(); // Événement pour la préparation terminée
		public UnityEvent            onStart             = new(); // Événement pour le début de la lecture
		public UnityEvent            onEnd               = new(); // Événement pour la fin de la lecture
		public UnityEvent<Exception> onError             = new(); // Événement pour les erreurs
		public UnityEvent            onConnectionLost    = new(); // Événement pour perte de connexion
		public UnityEvent            onReconnecting      = new(); // Événement pour tentative de reconnexion
		public UnityEvent            onReconnected       = new(); // Événement pour reconnexion réussie
		public UnityEvent            onPlay              = new(); // Événement pour la lecture
		public UnityEvent            onPause             = new(); // Événement pour la pause
		public UnityEvent            onResume            = new(); // Événement pour la reprise
		public UnityEvent<double>    onProgress          = new(); // Événement pour la progression (0.0 à 1.0)
		public UnityEvent<double>    onSeek              = new(); // Événement pour le seek (temps en secondes)
		public UnityEvent<float>     onVolumeChanged     = new(); // Événement pour le changement de volume (0.0 à 1.0)
		public UnityEvent<bool>      onPlayStatusChanged = new(); // Événement pour le changement de statut de lecture

		public void AddReadyListener(UnityAction listener)
			=> onReady.AddListener(listener);

		public void RemoveReadyListener(UnityAction listener)
			=> onReady.RemoveListener(listener);

		public void AddStartListener(UnityAction listener)
			=> onStart.AddListener(listener);

		public void RemoveStartListener(UnityAction listener)
			=> onStart.RemoveListener(listener);

		public void AddEndListener(UnityAction listener)
			=> onEnd.AddListener(listener);

		public void RemoveEndListener(UnityAction listener)
			=> onEnd.RemoveListener(listener);

		public void AddErrorListener(UnityAction<Exception> listener)
			=> onError.AddListener(listener);

		public void RemoveErrorListener(UnityAction<Exception> listener)
			=> onError.RemoveListener(listener);

		public void AddConnectionLostListener(UnityAction listener)
			=> onConnectionLost.AddListener(listener);

		public void RemoveConnectionLostListener(UnityAction listener)
			=> onConnectionLost.RemoveListener(listener);

		public void AddReconnectingListener(UnityAction listener)
			=> onReconnecting.AddListener(listener);

		public void RemoveReconnectingListener(UnityAction listener)
			=> onReconnecting.RemoveListener(listener);

		public void AddReconnectedListener(UnityAction listener)
			=> onReconnected.AddListener(listener);

		public void RemoveReconnectedListener(UnityAction listener)
			=> onReconnected.RemoveListener(listener);

		// Nouvelles méthodes pour les nouveaux événements
		public void AddPlayListener(UnityAction listener)
			=> onPlay.AddListener(listener);

		public void RemovePlayListener(UnityAction listener)
			=> onPlay.RemoveListener(listener);

		public void AddPauseListener(UnityAction listener)
			=> onPause.AddListener(listener);

		public void RemovePauseListener(UnityAction listener)
			=> onPause.RemoveListener(listener);

		public void AddResumeListener(UnityAction listener)
			=> onResume.AddListener(listener);

		public void RemoveResumeListener(UnityAction listener)
			=> onResume.RemoveListener(listener);

		public void AddProgressListener(UnityAction<double> listener)
			=> onProgress.AddListener(listener);

		public void RemoveProgressListener(UnityAction<double> listener)
			=> onProgress.RemoveListener(listener);

		public void AddSeekListener(UnityAction<double> listener)
			=> onSeek.AddListener(listener);

		public void RemoveSeekListener(UnityAction<double> listener)
			=> onSeek.RemoveListener(listener);

		public void AddVolumeChangedListener(UnityAction<float> listener)
			=> onVolumeChanged.AddListener(listener);

		public void RemoveVolumeChangedListener(UnityAction<float> listener)
			=> onVolumeChanged.RemoveListener(listener);

		public void AddPlayStatusChangedListener(UnityAction<bool> listener)
			=> onPlayStatusChanged.AddListener(listener);

		public void RemovePlayStatusChangedListener(UnityAction<bool> listener)
			=> onPlayStatusChanged.RemoveListener(listener);

		#endregion Events

		private       UnityVideoPlayer _videoPlayer;
		private       AudioSource      _audioSource;
		private       string           _currentUrl;
		private       bool             _isReconnecting;
		private       int              _reconnectAttempts;
		private const int              MaxReconnectAttempts = 3;
		private const float            ReconnectDelay       = 2;
		private       double           _lastProgress        = -1; // Pour éviter de déclencher l'événement en continu
		private       bool             _lastPlayingStatus;        // Pour détecter les changements de statut

		public UnityVideoPlayer UnityVideoPlayer {
			// ReSharper disable once Unity.PerformanceCriticalCodeInvocation
			get => _videoPlayer ??= GetComponent<UnityVideoPlayer>();
			set => _videoPlayer = value;
		}

		public AudioSource AudioSource {
			// ReSharper disable once Unity.PerformanceCriticalCodeInvocation
			get => _audioSource ??= GetComponent<AudioSource>();
			set => _audioSource = value;
		}

		private void Awake() {
			UnityVideoPlayer = GetComponent<UnityVideoPlayer>();
			AudioSource      = GetComponent<AudioSource>();

			UnityVideoPlayer.prepareCompleted    += OnPrepareCompleted;
			UnityVideoPlayer.started             += OnStarted;
			UnityVideoPlayer.loopPointReached    += OnLoopPointReached;
			UnityVideoPlayer.errorReceived       += OnErrorReceived;
			UnityVideoPlayer.clockResyncOccurred += OnClockResyncOccurred;
			UnityVideoPlayer.frameDropped        += OnFrameDropped;
			UnityVideoPlayer.frameReady          += OnFrameReady;
			UnityVideoPlayer.seekCompleted       += OnSeekCompleted;
		}

		private void OnDestroy() {
			UnityVideoPlayer.prepareCompleted    -= OnPrepareCompleted;
			UnityVideoPlayer.started             -= OnStarted;
			UnityVideoPlayer.loopPointReached    -= OnLoopPointReached;
			UnityVideoPlayer.errorReceived       -= OnErrorReceived;
			UnityVideoPlayer.clockResyncOccurred -= OnClockResyncOccurred;
			UnityVideoPlayer.frameDropped        -= OnFrameDropped;
			UnityVideoPlayer.frameReady          -= OnFrameReady;
			UnityVideoPlayer.seekCompleted       -= OnSeekCompleted;
		}

		private void Update() {
			// Vérification du statut de lecture
			var isPlaying = UnityVideoPlayer.isPlaying;
			if (isPlaying != _lastPlayingStatus) {
				_lastPlayingStatus = isPlaying;
				onPlayStatusChanged.Invoke(isPlaying);
			}

			// Surveiller le progrès de la vidéo seulement si elle est en cours de lecture
			if (isPlaying && UnityVideoPlayer.length > 0) {
				var currentProgress = GetProgress();
				// Déclencher l'événement seulement si le progrès a changé de manière significative (plus de 1%)
				if (Math.Abs(currentProgress - _lastProgress) > float.Epsilon) {
					_lastProgress = currentProgress;
					onProgress.Invoke(currentProgress);
				}
			}
		}

		public void Play(string url, bool loop = false) {
			_currentUrl        = url; // Stocker l'URL pour les reconnexions
			_reconnectAttempts = 0;   // Réinitialiser le compteur de tentatives
			_isReconnecting    = false;

			UnityVideoPlayer.url       = url;
			UnityVideoPlayer.isLooping = loop;
			UnityVideoPlayer.Prepare();

			onPlay.Invoke();
		}

		public void Stop() {
			if (!UnityVideoPlayer.isPlaying) return;
			UnityVideoPlayer.Stop();
		}

		public void Pause() {
			if (!UnityVideoPlayer.isPlaying) return;
			UnityVideoPlayer.Pause();
			onPause.Invoke();
		}

		public void Resume() {
			if (UnityVideoPlayer.isPlaying) return;
			UnityVideoPlayer.Play();
			onResume.Invoke();
		}

		public void SetVolume(float volume) {
			var clampedVolume = Mathf.Clamp01(volume);
			AudioSource.volume = clampedVolume;
			onVolumeChanged.Invoke(clampedVolume);
		}

		public void SetSeek(double time) {
			var clampedTime = Math.Clamp(time, 0, UnityVideoPlayer.length);
			UnityVideoPlayer.time = clampedTime;
			onSeek.Invoke(clampedTime);
		}

		public double GetTime()
			=> UnityVideoPlayer.time;

		public double GetProgress()
			=> UnityVideoPlayer.length > 0 ? UnityVideoPlayer.time / UnityVideoPlayer.length : 0;

		public double GetDuration()
			=> UnityVideoPlayer.length;

		public bool IsLooping()
			=> UnityVideoPlayer.isLooping;

		public void SetLooping(bool loop)
			=> UnityVideoPlayer.isLooping = loop;

		public RenderTexture GetRender()
			=> UnityVideoPlayer.targetTexture;

		public float GetVolume()
			=> AudioSource.volume;

		public bool IsPlaying()
			=> UnityVideoPlayer.isPlaying;

		private void OnPrepareCompleted(UnityVideoPlayer source) {
			Logger.Log($"Video prepared: {source.url}");

			// Si c'était une reconnexion réussie
			if (_isReconnecting) {
				_isReconnecting    = false;
				_reconnectAttempts = 0;
				Logger.Log("Reconnexion réussie !");
				onReconnected.Invoke();
			}

			onReady.Invoke();
		}

		private void OnStarted(UnityVideoPlayer source) {
			Logger.Log($"Video started: {source.url}");
			onStart.Invoke();
		}

		private void OnLoopPointReached(UnityVideoPlayer source) {
			Logger.Log($"Video ended: {source.url}");
			onEnd.Invoke();
		}

		private void OnErrorReceived(UnityVideoPlayer source, string message) {
			var exception = new Exception(message);
			Logger.LogError($"Video error: {source.url} - {message}");

			// Vérifier si c'est un problème de connexion
			if (IsConnectionError(message) && !string.IsNullOrEmpty(_currentUrl)) {
				HandleConnectionError();
			} else {
				// Erreur non liée à la connexion, propager l'erreur
				onError.Invoke(exception);
			}
		}

		private void OnSeekCompleted(UnityVideoPlayer source) {
			Logger.Log($"Seek completed on video: {source.url}");
		}

		private void OnClockResyncOccurred(UnityVideoPlayer source, double seconds) {
			Logger.Log($"Clock resync occurred: {seconds}s");
		}

		private void OnFrameDropped(UnityVideoPlayer source) {
			Logger.LogWarning("Frame dropped");
		}

		private void OnFrameReady(UnityVideoPlayer source, long frameIdx) {
			// Frame ready - peut être utilisé pour des traitements spécifiques
		}

		/// <summary>
		/// Détermine si l'erreur est liée à un problème de connexion
		/// </summary>
		private static bool IsConnectionError(string errorMessage) {
			var connectionErrors = new[] {
				"network", "connection", "timeout", "unreachable",
				"dns", "host", "failed to connect", "cannot connect",
				"internet", "offline", "502", "503", "504"
			};

			return connectionErrors
				.Any(error => errorMessage.ToLower().Contains(error));
		}

		/// <summary>
		/// Gère les erreurs de connexion avec tentatives de reconnexion
		/// </summary>
		private void HandleConnectionError() {
			if (!_isReconnecting) {
				Logger.Log("Perte de connexion détectée");
				onConnectionLost.Invoke();
			}

			if (_reconnectAttempts < MaxReconnectAttempts) {
				_reconnectAttempts++;
				_isReconnecting = true;

				Logger.Log($"Tentative de reconnexion {_reconnectAttempts}/{MaxReconnectAttempts}");
				onReconnecting.Invoke();

				AttemptReconnectionAsync().Forget();
			} else {
				Logger.LogError("Échec de la reconnexion après plusieurs tentatives");
				_isReconnecting = false;
				onError.Invoke(new Exception($"Impossible de se reconnecter après {MaxReconnectAttempts} tentatives"));
			}
		}

		/// <summary>
		/// Tente la reconnexion avec délai en utilisant UniTask
		/// </summary>
		private async UniTaskVoid AttemptReconnectionAsync() {
			await UniTask.Delay(TimeSpan.FromSeconds(ReconnectDelay), cancellationToken: this.GetCancellationTokenOnDestroy());

			if (!string.IsNullOrEmpty(_currentUrl)) {
				Logger.Log($"Reconnexion en cours vers: {_currentUrl}");
				UnityVideoPlayer.url = _currentUrl;
				UnityVideoPlayer.Prepare();
			}
		}

		/// <summary>
		/// Force une reconnexion manuelle
		/// </summary>
		public void ForceReconnect() {
			if (string.IsNullOrEmpty(_currentUrl)) return;
			_reconnectAttempts = 0;
			_isReconnecting    = true;
			onReconnecting.Invoke();
			AttemptReconnectionAsync().Forget();
		}
	}
}