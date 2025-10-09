using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Hactazia.VideoPlayer.Components {
	[DisallowMultipleComponent]
	[RequireComponent(typeof(AudioOutput))]
	[RequireComponent(typeof(AudioSource))]
	public sealed class AudioSourceOutput : MonoBehaviour {
		[SerializeField]
		private AudioOutput source;

		[SerializeField]
		private AudioSource targetAudioSource;

		[Tooltip("Duration in seconds that the internal buffer should keep queued samples.")]
		[SerializeField]
		[Range(0.25f, 5f)]
		private float bufferDurationSeconds = 1.5f;

		private readonly ConcurrentQueue<AudioOutput.AudioBuffer> _pendingBuffers = new();
		private readonly Queue<float>                             _sampleQueue    = new();
		private readonly object                                   _queueLock      = new();

		private AudioClip _clip;
		private int       _clipChannels;
		private int       _clipSampleRate;
		private int       _maxQueuedSamples;
		private bool      _subscribed;

		private void Awake() {
			source            ??= GetComponent<AudioOutput>();
			targetAudioSource ??= GetComponent<AudioSource>();
		}

		private void OnEnable()
			=> Subscribe();

		private void OnDisable()
			=> Unsubscribe();

		private void OnDestroy() {
			Unsubscribe();
			ReleaseClip();
		}

		private void Update() {
			ProcessPendingBuffers();
			EnsurePlayback();
		}

		private void Subscribe() {
			if (_subscribed || !source) return;

			source.OnSamplesReady += HandleSamplesReady;
			source.OnPaused       += HandlePaused;
			source.OnResumed      += HandleResumed;
			source.OnSeek         += HandleSeek;
			_subscribed           =  true;
		}

		private void Unsubscribe() {
			if (!_subscribed || !source) return;

			source.OnSamplesReady -= HandleSamplesReady;
			source.OnPaused       -= HandlePaused;
			source.OnResumed      -= HandleResumed;
			source.OnSeek         -= HandleSeek;
			_subscribed           =  false;
		}

		private void HandleSamplesReady(AudioOutput.AudioBuffer buffer) {
			if (buffer.Samples == null || buffer.Samples.Length == 0) return;
			_pendingBuffers.Enqueue(buffer);
		}

		private void HandlePaused() {
			if (!targetAudioSource) return;
			targetAudioSource.Pause();
		}

		private void HandleResumed() {
			if (!targetAudioSource || !_clip) return;

			targetAudioSource.UnPause();
			if (!targetAudioSource.isPlaying)
				targetAudioSource.Play();
		}

		private void HandleSeek() {
			ClearQueuedSamples();

			if (!targetAudioSource) return;
			targetAudioSource.Stop();
			targetAudioSource.timeSamples = 0;
		}

		private void ProcessPendingBuffers() {
			while (_pendingBuffers.TryDequeue(out var buffer)) {
				EnsureClip(buffer.Channels, buffer.SampleRate);
				EnqueueSamples(buffer.Samples);
			}
		}

		private void EnsureClip(int channels, int sampleRate) {
			if (_clip && _clipChannels == channels && _clipSampleRate == sampleRate) return;
			CreateOrRecreateClip(Mathf.Max(1, channels), Mathf.Max(1, sampleRate));
		}

		private void CreateOrRecreateClip(int channels, int sampleRate) {
			ReleaseClip();

			_clipChannels     = channels;
			_clipSampleRate   = sampleRate;
			_maxQueuedSamples = Mathf.CeilToInt(bufferDurationSeconds * sampleRate) * channels;
			
			if (_maxQueuedSamples <= 0)
				_maxQueuedSamples = sampleRate * channels;

			_clip = AudioClip.Create(
				"BufferedAudioClip",
				Mathf.Max(1, _maxQueuedSamples / channels),
				channels,
				sampleRate,
				true,
				OnAudioRead,
				OnAudioSetPosition
			);

			if (!targetAudioSource) return;

			targetAudioSource.clip        = _clip;
			targetAudioSource.loop        = true;
			targetAudioSource.playOnAwake = false;
		}

		private void ReleaseClip() {
			if (!_clip) return;

			if (targetAudioSource) {
				targetAudioSource.Stop();
				targetAudioSource.clip = null;
			}
			
			if (Application.isPlaying)
				Destroy(_clip);
			else DestroyImmediate(_clip);

			_clip             = null;
			_clipChannels     = 0;
			_clipSampleRate   = 0;
			_maxQueuedSamples = 0;

			ClearQueuedSamples();
		}

		private void EnqueueSamples(float[] samples) {
			if (samples == null || samples.Length == 0) return;

			lock (_queueLock) 
				foreach (var sample in samples) {
					_sampleQueue.Enqueue(sample);
					if (_maxQueuedSamples > 0 && _sampleQueue.Count > _maxQueuedSamples)
						_sampleQueue.Dequeue();
				}
		}

		private void ClearQueuedSamples() {
			lock (_queueLock) 
				_sampleQueue.Clear();
		}

		private void EnsurePlayback() {
			if (!targetAudioSource || !_clip) return;

			bool hasData;
			lock (_queueLock) 
				hasData = _sampleQueue.Count > _clipChannels;

			if (!targetAudioSource.isPlaying && hasData) 
				targetAudioSource.Play();
		}

		private void OnAudioRead(float[] data) {
			if (data == null || data.Length == 0) return;

			lock (_queueLock) 
				for (var i = 0; i < data.Length; i++) 
					data[i] = _sampleQueue.Count > 0 ? _sampleQueue.Dequeue() : 0f;
		}

		private void OnAudioSetPosition(int position) {
			// Reset playback pointer when AudioSource seeks.
		}
	}
}