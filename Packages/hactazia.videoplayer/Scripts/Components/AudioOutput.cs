using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using FFmpeg.AutoGen;
using UnityEngine;

namespace Hactazia.VideoPlayer.Components {
	[DisallowMultipleComponent]
	public sealed class AudioOutput : MonoBehaviour {
		public struct AudioBuffer {
			public float[] Samples;
			public int     Channels;
			public int     SampleRate;
			public int     SamplesPerChannel;
			public long    Pts;
		}

		public event Action<AudioBuffer> OnSamplesReady;
		public event Action              OnPaused;
		public event Action              OnResumed;
		public event Action              OnSeek;

		[Header("Runtime Data")]
		public long pts;

		public int Channels
			=> _channels;

		public int SampleRate
			=> _sampleRate;

		public AVSampleFormat SampleFormat
			=> _sampleFormat;

		private readonly Mutex _mutex = new();

		private int            _channels;
		private int            _sampleRate;
		private AVSampleFormat _sampleFormat;
		private bool           _initialized;
		private bool           _paused = true;
		private bool           _reportedUnsupportedFormat;

		public void Init(int sampleRate, int channels, AVSampleFormat sampleFormat) {
			if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
			if (channels   <= 0) throw new ArgumentOutOfRangeException(nameof(channels));

			if (_mutex.WaitOne())
				try {
					_sampleRate                = sampleRate;
					_channels                  = channels;
					_sampleFormat              = sampleFormat;
					pts                        = 0;
					_initialized               = true;
					_paused                    = false;
					_reportedUnsupportedFormat = false;
				} finally {
					_mutex.ReleaseMutex();
				}

			OnResumed?.Invoke();
		}

		public void Pause() {
			if (_mutex.WaitOne())
				try {
					_paused = true;
				} finally {
					_mutex.ReleaseMutex();
				}

			OnPaused?.Invoke();
		}

		public void Resume() {
			if (_mutex.WaitOne())
				try {
					if (!_initialized) return;
					_paused = false;
				} finally {
					_mutex.ReleaseMutex();
				}

			OnResumed?.Invoke();
		}

		public void Seek() {
			if (_mutex.WaitOne())
				try {
					pts = 0;
				} finally {
					_mutex.ReleaseMutex();
				}

			OnSeek?.Invoke();
		}

		public void QueueFrames(AVFrame[] frames, int count) {
			if (frames == null || count <= 0) return;
			if (!_initialized) return;

			bool paused;
			if (!_mutex.WaitOne())
				return;

			try {
				paused = _paused;
			} finally {
				_mutex.ReleaseMutex();
			}

			if (paused) return;

			var readyBuffers = new List<AudioBuffer>(count);

			for (var i = 0; i < count; i++) {
				var frame = frames[i];
				if (frame.format == -1 || frame.nb_samples <= 0) continue;

				var samples = ConvertFrame(ref frame);
				if (samples.Length == 0) continue;

				readyBuffers.Add(
					new AudioBuffer {
						Samples           = samples,
						Channels          = GetFrameChannels(frame),
						SampleRate        = frame.sample_rate > 0 ? frame.sample_rate : _sampleRate,
						SamplesPerChannel = frame.nb_samples,
						Pts               = frame.pts
					}
				);
			}

			if (readyBuffers.Count == 0)
				return;

			if (_mutex.WaitOne())
				try {
					pts = readyBuffers[^1].Pts;
				} finally {
					_mutex.ReleaseMutex();
				}

			foreach (var buffer in readyBuffers)
				OnSamplesReady?.Invoke(buffer);
		}

		private int GetFrameChannels(AVFrame frame) {
			var frameChannels = frame.ch_layout.nb_channels;
			if (frameChannels <= 0)
				frameChannels = _channels;
			return Mathf.Max(1, frameChannels);
		}

		private unsafe float[] ConvertFrame(ref AVFrame frame) {
			var channels          = GetFrameChannels(frame);
			var samplesPerChannel = Math.Max(0, frame.nb_samples);
			if (channels <= 0 || samplesPerChannel <= 0)
				return Array.Empty<float>();

			var totalSamples = samplesPerChannel * channels;
			var result       = new float[totalSamples];

			var format       = (AVSampleFormat)frame.format;
			var planar       = ffmpeg.av_sample_fmt_is_planar(format) == 1;
			var packedFormat = planar ? ffmpeg.av_get_packed_sample_fmt(format) : format;
			if (packedFormat == AVSampleFormat.AV_SAMPLE_FMT_NONE)
				packedFormat = format;

			try {
				switch (packedFormat) {
					case AVSampleFormat.AV_SAMPLE_FMT_FLT:

						if (!planar) {
							if (frame.data[0] == null) return Array.Empty<float>();
							Marshal.Copy((IntPtr)frame.data[0], result, 0, totalSamples);
						} else CopyPlanarFloats(ref frame, channels, samplesPerChannel, result);

						break;
					case AVSampleFormat.AV_SAMPLE_FMT_S16:

						if (!planar) {
							if (frame.data[0] == null) return Array.Empty<float>();
							var temp = new short[totalSamples];
							Marshal.Copy((IntPtr)frame.data[0], temp, 0, totalSamples);
							ShortsToFloats(temp, result, totalSamples);
						} else CopyPlanarShorts(ref frame, channels, samplesPerChannel, result);

						break;
					case AVSampleFormat.AV_SAMPLE_FMT_S32:

						if (!planar) {
							if (frame.data[0] == null) return Array.Empty<float>();
							var temp = new int[totalSamples];
							Marshal.Copy((IntPtr)frame.data[0], temp, 0, totalSamples);
							IntsToFloats(temp, result, totalSamples);
						} else CopyPlanarInts(ref frame, channels, samplesPerChannel, result);

						break;
					default:
						ReportUnsupportedFormat(format);
						return Array.Empty<float>();
				}
			} catch (Exception ex) {
				Debug.LogException(ex);
				return Array.Empty<float>();
			}

			return result;
		}

		private static unsafe void CopyPlanarFloats(ref AVFrame frame, int channels, int samplesPerChannel, float[] destination) {
			var temp = new float[samplesPerChannel];
			for (uint c = 0; c < channels; c++) {
				if (frame.data[c] == null) continue;
				Marshal.Copy((IntPtr)frame.data[c], temp, 0, samplesPerChannel);
				for (var i = 0; i < samplesPerChannel; i++)
					destination[i * channels + c] = temp[i];
			}
		}

		private static unsafe void CopyPlanarShorts(ref AVFrame frame, int channels, int samplesPerChannel, float[] destination) {
			var temp = new short[samplesPerChannel];
			for (uint c = 0; c < channels; c++) {
				if (frame.data[c] == null) continue;
				Marshal.Copy((IntPtr)frame.data[c], temp, 0, samplesPerChannel);
				for (var i = 0; i < samplesPerChannel; i++)
					destination[i * channels + c] = temp[i] / 32768f;
			}
		}

		private static unsafe void CopyPlanarInts(ref AVFrame frame, int channels, int samplesPerChannel, float[] destination) {
			var temp = new int[samplesPerChannel];
			for (uint c = 0; c < channels; c++) {
				if (frame.data[c] == null) continue;
				Marshal.Copy((IntPtr)frame.data[c], temp, 0, samplesPerChannel);
				for (var i = 0; i < samplesPerChannel; i++)
					destination[i * channels + c] = temp[i] / 2147483648f;
			}
		}

		private static void ShortsToFloats(short[] source, float[] destination, int length) {
			for (var i = 0; i < length; i++)
				destination[i] = source[i] / 32768f;
		}

		private static void IntsToFloats(int[] source, float[] destination, int length) {
			for (var i = 0; i < length; i++)
				destination[i] = source[i] / 2147483648f;
		}

		private void ReportUnsupportedFormat(AVSampleFormat format) {
			if (_reportedUnsupportedFormat) return;
			_reportedUnsupportedFormat = true;
			Debug.LogWarning($"AudioOutput does not support sample format: {format}");
		}
	}
}