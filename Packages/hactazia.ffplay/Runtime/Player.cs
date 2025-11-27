using System;
using System.IO;
using System.Threading;
using FFmpeg.AutoGen;
using FFmpeg.Unity.Helpers;
using Hactazia.FFPlay.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Hactazia.FFPlay {
	/// <summary>
	/// Optimized media player using a single demuxing thread for all streams.
	/// Replaces the original Player with 3 threads by a more efficient single-thread design.
	/// </summary>
	public class Player : MonoBehaviour {
		#region Events

		public readonly UnityEvent            OnPrepare   = new();
		public readonly UnityEvent<Exception> OnError     = new();
		public readonly UnityEvent            OnReady     = new();
		public readonly UnityEvent            OnEnded     = new();
		public readonly UnityEvent            OnStalled   = new();
		public readonly UnityEvent            OnUnstalled = new();
		public readonly UnityEvent<bool>      OnLooping   = new();
		public readonly UnityEvent<double>    OnSeeked    = new();
		public readonly UnityEvent<PlayState> OnPlayState = new();

		#endregion

		#region Fields

		private          IStreamTimings _timings;
		private          Thread         _demuxThread;
		private readonly object         _threadLock = new();

		public double videoOffset    = 0d;
		public double audioOffset    = 0d;
		public double subtitleOffset = 0d;

		public VideoWorker    videoWorker;
		public AudioWorker    audioWorker;
		public SubtitleWorker subtitleWorker;

		private double _timeOffset = 0d;
		private double _pauseTime  = 0d;

		// Frame buffer for audio
		private AVFrame[] _audioFrames = new AVFrame[500];

		#endregion

		#region Properties

		public double TimeAsDouble
			=> AudioSettings.dspTime;

		public double PlaybackTime
			=> IsPaused ? _pauseTime : TimeAsDouble - _timeOffset;

		public double VideoTime
			=> TimeAsDouble - _timeOffset + videoOffset;

		public double AudioTime
			=> TimeAsDouble - _timeOffset + audioOffset;

		public double SubtitleTime
			=> TimeAsDouble - _timeOffset + subtitleOffset;

		public bool IsPlaying { get; private set; }
		public bool IsStream  { get; private set; }
		public bool IsPaused  { get; private set; }

		public bool IsStalled
			=> false;
		// => _timings?.IsVideoStalled == true || _timings?.IsAudioStalled == true;

		private PlayState _currentPlayState = PlayState.Stopped;

		public PlayState CurrentPlayState {
			get => _currentPlayState;
			private set {
				if (_currentPlayState == value) return;
				_currentPlayState = value;
				OnPlayState.Invoke(value);
			}
		}

		private bool _isLooping;

		public bool IsLooping {
			get => _isLooping;
			set {
				if (_isLooping == value) return;
				_isLooping = value;
				OnLooping.Invoke(value);
			}
		}

		public bool HasVideo
			=> _timings?.Has(MediaType.Video) ?? false;

		public bool HasAudio
			=> _timings?.Has(MediaType.Audio) ?? false;

		public bool HasSubtitle
			=> _timings?.Has(MediaType.Subtitle) ?? false;

		/// <summary>
		/// Exposes the internal timing system for debugging purposes.
		/// </summary>
		public IStreamTimings Timings
			=> _timings;

		#endregion


		#region Playback Control

		public void Play(Stream stream) {
			IsPlaying = false;

			StopThread();
			DisposeTimings();

			_timings = new MultiStreamTimings(new MediaSource(stream));

			Initialize();
		}

		public void Play(string url) {
			IsPlaying = false;

			StopThread();
			DisposeTimings();

			_timings = new MultiStreamTimings(new MediaSource(url));

			Initialize();
		}

		/// <summary>
		/// Plays media with separate sources for video, audio, and subtitles.
		/// </summary>
		public void Play(string videoUrl, string audioUrl, string subtitleUrl = null) {
			IsPlaying = false;

			StopThread();
			DisposeTimings();

			Debug.LogWarning("SeparateStreamTimings with URLs is not implemented.");
			// _timings = new SeparateStreamTimings(videoUrl, audioUrl, subtitleUrl);
			_timings = new MultiStreamTimings(new MediaSource(videoUrl));

			Initialize();
		}

		/// <summary>
		/// Plays media with separate streams for video, audio, and subtitles.
		/// </summary>
		public void Play(Stream videoStream, Stream audioStream, Stream subtitleStream = null) {
			IsPlaying = false;

			StopThread();
			DisposeTimings();

			Debug.LogWarning("SeparateStreamTimings with URLs is not implemented.");
			// _timings = new SeparateStreamTimings(videoStream, audioStream, subtitleStream);
			_timings = new MultiStreamTimings(new MediaSource(videoStream));

			Initialize();
		}

		private void Initialize() {
			OnPrepare.Invoke();

			// Initialize audio worker
			if (_timings.Has(MediaType.Audio) && audioWorker) {
				var decoder = _timings.Get(MediaType.Audio);
				audioWorker.Init(decoder.SampleRate, decoder.Channels, decoder.SampleFormat);
			}

			// Determine if stream or file
			if (_timings.Has(MediaType.Video)) {
				_timeOffset = TimeAsDouble - _timings.StartTime;
				IsStream = Math.Abs(_timings.StartTime - ffmpeg.AV_NOPTS_VALUE) < float.Epsilon
					|| Math.Abs(_timings.StartTime)                             > 5d;
			} else _timeOffset = TimeAsDouble;

			// Validate streams
			if (!_timings.Has(MediaType.Video) && !_timings.Has(MediaType.Audio)) {
				IsPaused  = true;
				IsPlaying = false;
				OnError.Invoke(new Exception("No valid audio or video stream found."));
				return;
			}

			OnReady.Invoke();

			// Reset and start workers
			foreach (var w in GetWorkers()) {
				w?.Seek();
				w?.Resume();
			}

			// _timings?.ResetStallDetection();
			RunThread();
			IsPlaying        = true;
			CurrentPlayState = PlayState.Playing;
		}

		[ContextMenu("Pause")]
		public void Pause() {
			if (IsPaused) return;

			_pauseTime = PlaybackTime;
			foreach (var w in GetWorkers())
				w?.Pause();

			IsPaused = true;
			StopThread();
			IsPlaying        = false;
			CurrentPlayState = PlayState.Paused;
		}

		[ContextMenu("Stop")]
		public void Stop() {
			Pause();
			Seek(0);
			_pauseTime       = 0d;
			CurrentPlayState = PlayState.Stopped;
		}

		[ContextMenu("Resume")]
		public void Resume() {
			if (!IsPaused) return;

			StopThread();
			_timeOffset = TimeAsDouble - _pauseTime;

			foreach (var w in GetWorkers())
				w?.Resume();

			IsPaused = false;
			// _timings?.ResetStallDetection();
			RunThread();
			IsPlaying        = true;
			CurrentPlayState = PlayState.Playing;
		}

		public void Seek(double timestamp) {
			if (IsStream || _timings == null) return;

			StopThread();
			_timeOffset = TimeAsDouble - timestamp;
			_pauseTime  = timestamp;

			_timings.Seek(timestamp);

			foreach (var w in GetWorkers())
				w?.Seek();

			// _timings?.ResetStallDetection();
			if (!IsPaused) RunThread();
			OnSeeked.Invoke(timestamp);
		}

		public double GetLength()
			=> _timings?.Length ?? 0d;

		public double GetCurrentTime()
			=> PlaybackTime;

		public string GetCurrentUrl()
			=> null; // Not easily accessible with new architecture

		#endregion

		#region Thread Management

		private void RunThread() {
			lock (_threadLock) {
				if (_demuxThread is { IsAlive: true })
					throw new Exception("Thread is already running");

				IsPaused = false;

				_demuxThread = new Thread(DemuxLoop) {
					Name         = "PlayerV2_Demux",
					IsBackground = true
				};
				_demuxThread.Start();
			}
		}

		private void StopThread() {
			lock (_threadLock) {
				var paused = IsPaused;
				IsPaused = true;

				if (_demuxThread is { IsAlive: true })
					_demuxThread.Join(1000);

				_demuxThread = null;
				IsPaused     = paused;
			}
		}

		/// <summary>
		/// Single thread that handles all stream types
		/// </summary>
		private void DemuxLoop() {
			while (!IsPaused) {
				try {
					if (_timings == null) continue;

					// Update timings with current playback positions
					_timings.Update(MediaType.Video, VideoTime);
					_timings.Update(MediaType.Audio, AudioTime);
					_timings.Update(MediaType.Subtitle, SubtitleTime);

					// Process video
					if (videoWorker && _timings.Has(MediaType.Video)) {
						var frame = _timings.GetFrame(MediaType.Video);
						if (frame.format != -1) videoWorker.PlayPacket(frame);
					}

					// Process audio (batch for efficiency)
					if (audioWorker && _timings.Has(MediaType.Audio)) {
						var count = _timings.GetFrames(MediaType.Audio, ref _audioFrames);
						if (count > 0) audioWorker.PlayPackets(_audioFrames, count);
					}

					// Process subtitles
					// if (_timings.HasSubtitle && subtitleWorker != null) {
					// 	var packet = _timings.GetSubtitlePacket();
					// 	if (packet.size > 0)
					// 		subtitleWorker.PlayPacket(null, packet); // Note: needs Timings compatibility
					// }
				} catch (Exception e) {
					Debug.LogException(e);
					break;
				}

				// Small sleep to prevent CPU spinning
				Thread.Sleep(1);
			}
		}

		#endregion

		#region Update & Stall Detection

		private void Update() {
			if (IsPaused) return;

			// Update stall detection in timing system (must be called from main thread)
			// _timings?.UpdateMainThread();

			// Check for end of file
			if (_timings is { IsEnd: true }) {
				if (IsLooping && !IsStream) {
					Seek(0);
					OnEnded.Invoke();
					return;
				}

				Pause();
				CurrentPlayState = PlayState.Ended;
				OnEnded.Invoke();
				return;
			}
		}

		#endregion

		#region Helpers

		private BaseWorker[] GetWorkers()
			=> new BaseWorker[] { videoWorker, audioWorker, subtitleWorker };

		private void DisposeTimings() {
			_timings?.Dispose();
			_timings = null;
		}

		#endregion

		#region Lifecycle

		private void OnEnable() {
			// Thread will be started when Play is called
		}

		private void OnDisable() {
			IsPaused = true;
			DisposeTimings();
		}

		private void OnDestroy() {
			StopThread();
			DisposeTimings();
		}

		#endregion
	}
}