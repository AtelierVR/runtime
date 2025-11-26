using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using FFmpeg.AutoGen;
using FFmpeg.Unity;
using FFmpeg.Unity.Helpers;
using Hactazia.FFPlay.Helpers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Hactazia.FFPlay
{
	public class Player : MonoBehaviour
	{
		public readonly UnityEvent OnPrepare = new();
		public readonly UnityEvent<Exception> OnError = new();
		public readonly UnityEvent OnReady = new();
		public readonly UnityEvent OnEnded = new();


		private Timings _videot;
		private Timings _audiot;
		private Timings _subtlt;


		public double timeAsDouble
			=> AudioSettings.dspTime;

		public double PlaybackTime
			=> IsPaused ? _pauseTime : timeAsDouble - _timeOffset;

		public double VideoTime
			=> timeAsDouble - _timeOffset + videoOffset;

		public double AudioTime
			=> timeAsDouble - _timeOffset + audioOffset;

		public double SubtitleTime
			=> timeAsDouble - _timeOffset + subtitleOffset;


		public IEnumerable<Timings> GetTimings()
			=> new[] { _videot, _audiot, _subtlt };

		public IEnumerable<Thread> GetThreads()
			=> new[] { _videoq, _audioq, _subtlq };

		public IEnumerable<BaseWorker> GetWorkers()
			=> new BaseWorker[] { videoWorker, audioWorker, subtitleWorker };

		private Thread _videoq;
		private Thread _audioq;
		private Thread _subtlq;

		private readonly object _threadLock = new();

		public double videoOffset = 0d;
		public double audioOffset = 0d;
		public double subtitleOffset = 0d;

		public VideoWorker videoWorker;
		public AudioWorker audioWorker;
		public SubtitleWorker subtitleWorker;

		private double _timeOffset = 0d;
		private double _pauseTime = 0d;

		public bool IsPlaying { get; private set; } = false;
		public bool IsStream { get; private set; } = false;

		public bool IsPaused { get; private set; } = false;

		public void Play(Stream streamV, Stream streamA = null, Stream streamS = null)
		{
			IsPlaying = false;

			StopThread();
			OnDestroy();

			var vContext = new Context(streamV);

			var aContext = streamA == null || streamV == streamA
				? vContext
				: new Context(streamA);

			var sContext = streamS == null || streamS == streamV
				? vContext
				: streamS == streamA
					? aContext
					: new Context(streamS);

			_videot = new Timings(vContext, AVMediaType.AVMEDIA_TYPE_VIDEO);
			_audiot = new Timings(aContext, AVMediaType.AVMEDIA_TYPE_AUDIO);
			_subtlt = new Timings(sContext, AVMediaType.AVMEDIA_TYPE_SUBTITLE);

			Init();
		}

		public void Play(string urlV, string urlA = null, string urlS = null)
		{
			IsPlaying = false;

			StopThread();
			OnDestroy();

			var vContext = new Context(urlV);
			var aContext = new Context(urlA);
			var sContext = new Context(urlS);

			// var aContext = string.IsNullOrEmpty(urlA) || urlV == urlA
			// 	? vContext
			// 	: new Context(urlA);
			//
			// var sContext = string.IsNullOrEmpty(urlS) || urlS == urlV
			// 	? vContext
			// 	: urlS == urlA
			// 		? aContext
			// 		: new Context(urlS);

			_videot = new Timings(vContext, AVMediaType.AVMEDIA_TYPE_VIDEO);
			_audiot = new Timings(aContext, AVMediaType.AVMEDIA_TYPE_AUDIO);
			_subtlt = new Timings(sContext, AVMediaType.AVMEDIA_TYPE_SUBTITLE);

			Init();
		}

		private void Init()
		{
			OnPrepare.Invoke();

			// Initialize audio player
			if (_audiot is { IsInputValid: true })
				audioWorker.Init(_audiot.Decoder.SampleRate, _audiot.Decoder.Channels, _audiot.Decoder.SampleFormat);


			// Determine if stream or file
			if (_videot is { IsInputValid: true })
			{
				_timeOffset = timeAsDouble - _videot.StartTime;
				IsStream = Math.Abs(_videot.StartTime - ffmpeg.AV_NOPTS_VALUE) < float.Epsilon
					|| Math.Abs(_videot.StartTime) > 5d;
			}
			else _timeOffset = timeAsDouble;

			// Ensure at least one valid stream
			if (!_videot.IsInputValid && !_audiot.IsInputValid)
			{
				IsPaused = true;
				StopThread();
				IsPlaying = false;
				OnError.Invoke(new Exception("No valid audio or video stream found."));
				return;
			}

			OnReady.Invoke();
			foreach (var p in GetWorkers())
			{
				p.Seek();
				p.Resume();
			}

			RunThread();
			IsPlaying = true;
		}

		public void Seek(double timestamp)
		{
			if (IsStream)
				return;

			StopThread();
			_timeOffset = timeAsDouble - timestamp;
			_pauseTime = timestamp;

			_videot?.Seek(VideoTime);

			_audiot?.Seek(AudioTime);

			_subtlt?.Seek(SubtitleTime);

			foreach (var p in GetWorkers())
				p.Seek();

			RunThread();
		}

		public double GetLength()
			=> (from t in GetTimings()
				where t is { IsInputValid: true }
				select t.GetLength)
				.FirstOrDefault();

		[ContextMenu("Pause")]
		public void Pause()
		{
			if (IsPaused) return;
			_pauseTime = PlaybackTime;
			foreach (var p in GetWorkers())
				p.Pause();
			IsPaused = true;
			StopThread();
			IsPlaying = false;
		}

		[ContextMenu("Resume")]
		public void Resume()
		{
			if (!IsPaused)
				return;
			StopThread();
			_timeOffset = timeAsDouble - _pauseTime;
			foreach (var p in GetWorkers())
				p.Resume();
			IsPaused = false;
			RunThread();
			IsPlaying = true;
		}

		private void Update()
		{
			if (IsPaused) return;
			foreach (var t in GetTimings())
				if (t is { IsEndOfFile: true })
				{
					Pause();
					OnEnded.Invoke();
				}
		}

		private void VideoThread()
		{
			while (!IsPaused)
			{
				Thread.Yield();

				try
				{
					if (_videot == null) continue;
					_videot.Update(VideoTime);
					videoWorker.PlayPacket(_videot.GetFrame());
				}
				catch (Exception e)
				{
					Debug.LogException(e);
					break;
				}
			}
		}


		private AVFrame[] _frames = new AVFrame[500];

		private void AudioThread()
		{
			while (!IsPaused)
			{
				Thread.Yield();

				try
				{
					if (_audiot == null) continue;
					_audiot.Update(AudioTime);
					var frameCount = _audiot.GetFramesNonAlloc(500, ref _frames);
					audioWorker.PlayPackets(_frames, frameCount);
				}
				catch (Exception e)
				{
					Debug.LogException(e);
					break;
				}
			}
		}

		private void SubtitleThread()
		{
			while (!IsPaused)
			{
				Thread.Yield();

				try
				{
					if (_subtlt == null) continue;
					_subtlt.Update(SubtitleTime);
					subtitleWorker.PlayPacket(_subtlt, _subtlt.GetPacket());
				}
				catch (Exception e)
				{
					Debug.LogException(e);
					break;
				}
			}
		}


		private void OnDestroy()
		{
			StopThread();
			foreach (var t in GetTimings())
				t?.Dispose();
			_videot = null;
			_audiot = null;
			_subtlt = null;
		}

		private void RunThread()
		{
			lock (_threadLock)
			{
				if (_videoq is { IsAlive: true } || _audioq is { IsAlive: true } || _subtlq is { IsAlive: true })
					throw new Exception("Threads are already running");

				IsPaused = false;

				_videoq = new Thread(VideoThread) { Name = nameof(VideoThread) };
				_audioq = new Thread(AudioThread) { Name = nameof(AudioThread) };
				_subtlq = new Thread(SubtitleThread) { Name = nameof(SubtitleThread) };

				_videoq.Start();
				_audioq.Start();
				_subtlq.Start();
			}
		}

		private void StopThread()
		{
			lock (_threadLock)
			{
				var paused = IsPaused;
				IsPaused = true;
				foreach (var t in GetThreads())
					if (t is { IsAlive: true })
						t.Join();
				IsPaused = paused;
			}
		}

		public void OnEnable()
		{
			lock (_threadLock)
			{
				_videoq = new Thread(VideoThread) { Name = nameof(VideoThread) };
				_audioq = new Thread(AudioThread) { Name = nameof(AudioThread) };
				_subtlq = new Thread(SubtitleThread) { Name = nameof(SubtitleThread) };
			}
		}

		public void OnDisable()
		{
			IsPaused = true;
			OnDestroy();
		}
	}
}