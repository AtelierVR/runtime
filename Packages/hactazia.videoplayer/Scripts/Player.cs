using System;
using System.Threading;
using FFmpeg.AutoGen;
using Hactazia.VideoPlayer.Components;
using Hactazia.VideoPlayer.Core;
using UnityEngine;

namespace Hactazia.VideoPlayer {
	[DisallowMultipleComponent]
	public sealed class Player : MonoBehaviour {
		[Header("Source")]
		[SerializeField]
		private string url;

		[SerializeField]
		private bool playOnAwake = true;

		[SerializeField]
		private bool loop;

		[Header("Offsets")]
		[SerializeField]
		private double videoOffset;

		[SerializeField]
		private double audioOffset;

		[SerializeField, Range(0.05f, 1f)]
		private float maxAudioDelta = 0.5f;

		[Header("Components")]
		[SerializeField]
		private VideoOutput videoOutput;

		[SerializeField]
		private AudioOutput audioOutput;

		public event Action<Player>         OnPrepareCompleted;
		public event Action<Player>         OnStarted;
		public event Action<Player>         OnSeekCompleted;
		public event Action<Player>         OnLoopPointReached;
		public event Action<Player, long>   OnFrameReady;
		public event Action<Player>         OnFrameDropped;
		public event Action<Player, double> OnClockResyncOccurred;
		public event Action<Player, string> OnErrorReceived;

		private          Framing _videoTimings;
		private          Framing _audioTimings;
		private          Thread    _videoThread;
		private          Thread    _audioThread;
		private readonly AVFrame[] _audioFrames = new AVFrame[256];

		private double _timeOffset;
		private double _pauseTime;
		private bool   _disposed;

		public bool IsPlaying { get; private set; }
		public bool IsPaused  { get; private set; } = true;
		public bool IsStream  { get; private set; }

		private double AudioSystemTime
			=> AudioSettings.dspTime;

		private double PlaybackTime
			=> IsPaused ? _pauseTime : AudioSystemTime - _timeOffset;

		private double VideoTime
			=> AudioSystemTime - _timeOffset + videoOffset;

		private double AudioTime
			=> AudioSystemTime - _timeOffset + audioOffset;

		public string GetUrl()
			=> url;

		private void Awake() {
			FFmpegInitializer.EnsureInitialized();
			videoOutput ??= GetComponentInChildren<VideoOutput>(true);
			audioOutput ??= GetComponentInChildren<AudioOutput>(true);
		}

		private void Start() {
			if (playOnAwake && !string.IsNullOrWhiteSpace(url))
				Play(url);
		}

		public void Play(string source) {
			Play(source, loop);
		}

		public void Play(string source, bool shouldLoop) {
			loop = shouldLoop;
			url  = source;

			if (string.IsNullOrWhiteSpace(source)) {
				RaiseError(new ArgumentException("URL must be provided", nameof(source)));
				return;
			}

			BeginPlayback(
				() => new Framing(source, AVMediaType.AVMEDIA_TYPE_VIDEO),
				() => new Framing(source, AVMediaType.AVMEDIA_TYPE_AUDIO)
			);
		}

		public void Play(string videoUrl, string audioUrl) {
			Play(videoUrl, audioUrl, loop);
		}

		public void Play(string videoUrl, string audioUrl, bool shouldLoop) {
			loop = shouldLoop;
			url  = videoUrl;

			if (string.IsNullOrWhiteSpace(videoUrl) && string.IsNullOrWhiteSpace(audioUrl)) {
				RaiseError(new ArgumentException("At least one of videoUrl or audioUrl must be provided."));
				return;
			}

			BeginPlayback(
				string.IsNullOrWhiteSpace(videoUrl) ? null : () => new Framing(videoUrl, AVMediaType.AVMEDIA_TYPE_VIDEO),
				string.IsNullOrWhiteSpace(audioUrl) ? null : () => new Framing(audioUrl, AVMediaType.AVMEDIA_TYPE_AUDIO)
			);
		}

		private void BeginPlayback(Func<Framing> videoFactory, Func<Framing> audioFactory) {
			StopPlaybackInternal();

			try {
				_videoTimings = videoFactory?.Invoke();
				if (_videoTimings is { IsInputValid: false }) {
					_videoTimings.Dispose();
					_videoTimings = null;
				}

				_audioTimings = audioFactory?.Invoke();
				if (_audioTimings is { IsInputValid: false }) {
					_audioTimings.Dispose();
					_audioTimings = null;
				}

				if (_audioTimings?.IsInputValid == true && audioOutput) {
					audioOutput.Init(_audioTimings.Decoder.SampleRate, _audioTimings.Decoder.Channels, _audioTimings.Decoder.SampleFormat);
				}

				if ((_videoTimings == null || !_videoTimings.IsInputValid) && (_audioTimings == null || !_audioTimings.IsInputValid)) {
					throw new InvalidOperationException("No playable audio or video streams found.");
				}

				InitializePlaybackState();
				StartThreads();
				audioOutput?.Resume();
				IsPlaying = true;
				IsPaused  = false;
				OnPrepareCompleted?.Invoke(this);
				OnStarted?.Invoke(this);
			} catch (Exception ex) {
				Debug.LogException(ex);
				RaiseError(ex);
				StopPlaybackInternal();
			}
		}

		public void Pause() {
			if (IsPaused) {
				return;
			}

			_pauseTime = PlaybackTime;
			IsPaused   = true;
			IsPlaying  = false;
			StopThreads();
			audioOutput?.Pause();
		}

		public void Resume() {
			if (!IsPaused) {
				return;
			}

			_timeOffset = AudioSystemTime - _pauseTime;
			IsPaused    = false;
			IsPlaying   = true;
			audioOutput?.Resume();
			StartThreads();
			OnStarted?.Invoke(this);
			RaiseClockResync(PlaybackTime);
		}

		public void Stop() {
			StopPlaybackInternal();
		}

		public void Seek(double timestamp) {
			if (IsStream) {
				return; // Streams cannot seek reliably
			}

			StopThreads();
			_timeOffset = AudioSystemTime - timestamp;
			_pauseTime  = timestamp;
			_videoTimings?.Seek(timestamp + videoOffset);
			_audioTimings?.Seek(timestamp + audioOffset);
			audioOutput?.Seek();
			StartThreads();
			OnSeekCompleted?.Invoke(this);
			RaiseClockResync(PlaybackTime);
		}

		public double GetLength() {
			double duration = 0d;
			if (_videoTimings?.IsInputValid == true) {
				duration = Math.Max(duration, _videoTimings.GetLength());
			}

			if (_audioTimings?.IsInputValid == true) {
				duration = Math.Max(duration, _audioTimings.GetLength());
			}

			return duration;
		}

		public double GetPlaybackTime()
			=> PlaybackTime;

		public void SetLoop(bool shouldLoop)
			=> loop = shouldLoop;

		public bool IsLooping()
			=> loop;

		private void RaiseClockResync(double seconds)
			=> OnClockResyncOccurred?.Invoke(this, seconds);

		private void RaiseError(Exception exception) {
			OnErrorReceived?.Invoke(this, exception?.Message ?? string.Empty);
		}

		private void InitializePlaybackState() {
			double startTime = 0d;
			if (_videoTimings?.IsInputValid == true) {
				startTime = _videoTimings.StartTime;
			} else if (_audioTimings?.IsInputValid == true) {
				startTime = _audioTimings.StartTime;
			}

			IsStream    = Math.Abs(startTime) > 5d;
			_timeOffset = AudioSystemTime - startTime;
			IsPaused    = false;
			_pauseTime  = 0d;
			RaiseClockResync(PlaybackTime);
		}

		private void StartThreads() {
			StopThreads();
			IsPaused = false;

			if (_videoTimings?.IsInputValid == true) {
				_videoThread = new Thread(VideoLoop) { Name = "VideoDecodeThread", IsBackground = true };
				_videoThread.Start();
			}

			if (_audioTimings?.IsInputValid == true) {
				_audioThread = new Thread(AudioLoop) { Name = "AudioDecodeThread", IsBackground = true };
				_audioThread.Start();
			}
		}

		private void StopThreads() {
			IsPaused = true;
			if (_videoThread != null && _videoThread.IsAlive) {
				try {
					_videoThread.Join();
				} catch (ThreadStateException) { }
			}

			if (_audioThread != null && _audioThread.IsAlive) {
				try {
					_audioThread.Join();
				} catch (ThreadStateException) { }
			}

			_videoThread = null;
			_audioThread = null;
		}

		private void StopPlaybackInternal() {
			StopThreads();
			IsPlaying   = false;
			IsPaused    = true;
			_pauseTime  = 0d;
			_timeOffset = AudioSystemTime;
			audioOutput?.Pause();
			audioOutput?.Seek();

			_audioTimings?.Dispose();
			_audioTimings = null;
			_videoTimings?.Dispose();
			_videoTimings = null;
		}

		private void Update() {
			if (IsPaused || !IsPlaying) {
				return;
			}

			bool videoEnded = _videoTimings != null && _videoTimings.IsEndOfFile();
			bool audioEnded = _audioTimings != null && _audioTimings.IsEndOfFile();

			if (IsStream) {
				return; // Live streams can momentarily signal EOF during ads; keep the threads running.
			}

			if (videoEnded || audioEnded) {
				HandlePlaybackCompleted();
			}
		}

		private void HandlePlaybackCompleted() {
			OnLoopPointReached?.Invoke(this);
			if (loop) {
				Seek(0d);
				Resume();
			} else Pause();
		}

		private void VideoLoop() {
			try {
				while (!IsPaused && !_disposed) {
					Thread.Yield();

					if (_videoTimings is not { IsInputValid: true })
						continue;

					_videoTimings.Update(VideoTime);
					var frame = _videoTimings.GetFrame();
					if (frame.format != -1) {
						videoOutput?.PresentFrame(frame);
						OnFrameReady?.Invoke(this, frame.pts);
					} else OnFrameDropped?.Invoke(this);
				}
			} catch (Exception ex) {
				Debug.LogException(ex);
				RaiseError(ex);
			}
		}

		private void AudioLoop() {
			try {
				while (!IsPaused && !_disposed) {
					Thread.Yield();

					if (_audioTimings is not { IsInputValid: true })
						continue;

					_audioTimings.Update(AudioTime);
					var count = _audioTimings.GetFramesNonAlloc(maxAudioDelta, _audioFrames);
					if (count <= 0) continue;
					audioOutput?.QueueFrames(_audioFrames, count);
				}
			} catch (Exception ex) {
				Debug.LogException(ex);
				RaiseError(ex);
			}
		}

		private void OnDisable() {
			StopPlaybackInternal();
		}

		private void OnDestroy() {
			_disposed = true;
			StopPlaybackInternal();
		}

		public VideoOutput GetVideoOutput()
			=> videoOutput;

		public AudioOutput GetAudioOutput()
			=> audioOutput;
	}
}