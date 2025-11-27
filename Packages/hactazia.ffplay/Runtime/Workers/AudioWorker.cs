using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using FFmpeg.Unity.Helpers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Hactazia.FFPlay {
	public class AudioWorker : BaseWorker {
		public readonly UnityEvent                               OnResume       = new();
		public readonly UnityEvent                               OnPause        = new();
		public readonly UnityEvent                               OnSeek         = new();
		public readonly UnityEvent<float>                        OnVolumeChange = new();
		public readonly UnityEvent<ICollection<float>, int, int> AddQueue       = new();

		public           float          bufferSize = 2f;
		private          int            _channels;
		private          float          _volume = 1f;
		private          int            _frequency;
		private          AVSampleFormat _sampleFormat;
		private readonly List<float>    _pcm = new();

		public void Init(int freq, int chl, AVSampleFormat splFor) {
			_channels     = chl;
			_sampleFormat = splFor;
			_frequency    = freq;
		}

		public override void Pause()
			=> OnPause.Invoke();

		public override void Resume()
			=> OnResume.Invoke();

		public override void Seek()
			=> OnSeek.Invoke();

		public void SetVolume(float volume) {
			_volume = Mathf.Clamp01(volume);
			OnVolumeChange.Invoke(_volume);
		}

		public float GetVolume()
			=> _volume;

		public void PlayPackets(ICollection<AVFrame> frames, int frameCount = -1) {
			if (frameCount == -1)
				frameCount = frames.Count;

			if (frameCount == 0 || frames.Count == 0) return;
			Debug.Log($"AudioWorker: PlayPackets {frameCount} frames");

			foreach (var frame in frames) {
				if (frameCount == 0) break;
				frameCount--;
				QueuePacket(frame);
			}
		}

		private unsafe void QueuePacket(AVFrame frame) {
			_pcm.Clear();
			pts = frame.pts;

			var size = ffmpeg.av_samples_get_buffer_size(null, 1, frame.nb_samples, _sampleFormat, 1);
			if (size < 0)
				return;

			var sampleCount = size / sizeof(float);

			for (uint ch = 0; ch < _channels; ch++) {
				var bb2 = new byte[size];
				var bb3 = new float[sampleCount];
				Marshal.Copy((IntPtr)frame.data[ch], bb2, 0, size);
				Buffer.BlockCopy(bb2, 0, bb3, 0, bb2.Length);

				if (ch == 0) {
					for (var i = 0; i < sampleCount; i++)
						_pcm.Add(bb3[i]);
				} else {
					for (var i = 0; i < sampleCount; i++)
						_pcm[i] = (_pcm[i] * ch + bb3[i]) / (ch + 1);
				}
			}

			AddQueue.Invoke(_pcm, 1, _frequency);
		}
	}
}