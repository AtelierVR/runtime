using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Hactazia.FFPlay {
	[RequireComponent(typeof(AudioSource))]
	public class BufferAudioSource : MonoBehaviour {
		public AudioWorker worker;

		[HideInInspector]
		public AudioSource audioSource;

		private AudioClip _audioClip;
		private int _writePosition;
		private int _channels;
		private int _frequency;
		private const int BufferLengthSeconds = 10;
		
		private readonly ConcurrentQueue<(float[] samples, int channels, int frequency)> _pendingBuffers = new();
		private volatile bool _seekRequested;
		private volatile bool _pauseRequested;
		private volatile bool _resumeRequested;
		private volatile float _pendingVolume = -1f;

		public void Awake() {
			worker ??= GetComponentInParent<AudioWorker>();

			if (!worker) {
				Debug.LogError($"[{nameof(BufferAudioSource)}] No AudioWorker found in parent hierarchy.", this);
				enabled = false;
				return;
			}

			audioSource = GetComponent<AudioSource>();
			audioSource.loop = true;
			
			worker.OnSeek.AddListener(HandleSeek);
			worker.AddQueue.AddListener(HandleAudioBuffer);
			worker.OnPause.AddListener(HandlePause);
			worker.OnResume.AddListener(HandleResume);
			worker.OnVolumeChange.AddListener(HandleVolumeChange);
		}

		public void OnDestroy() {
			worker?.OnSeek.RemoveListener(HandleSeek);
			worker?.AddQueue.RemoveListener(HandleAudioBuffer);
			worker?.OnPause.RemoveListener(HandlePause);
			worker?.OnResume.RemoveListener(HandleResume);
			worker?.OnVolumeChange.RemoveListener(HandleVolumeChange);
			
			if (_audioClip != null) {
				Destroy(_audioClip);
				_audioClip = null;
			}
		}

		private void Update() {
			// Handle seek on main thread
			if (_seekRequested) {
				_seekRequested = false;
				_writePosition = 0;
				while (_pendingBuffers.TryDequeue(out _)) { } // Clear pending buffers
				if (audioSource != null) {
					audioSource.Stop();
					audioSource.time = 0;
				}
			}

			// Handle pause on main thread
			if (_pauseRequested) {
				_pauseRequested = false;
				if (audioSource != null && audioSource.isPlaying) {
					audioSource.Pause();
				}
			}

			// Handle resume on main thread
			if (_resumeRequested) {
				_resumeRequested = false;
				if (audioSource != null && !audioSource.isPlaying && _audioClip != null) {
					audioSource.UnPause();
				}
			}

			// Handle volume change on main thread
			if (_pendingVolume >= 0f) {
				if (audioSource != null) {
					audioSource.volume = _pendingVolume;
				}
				_pendingVolume = -1f;
			}

			// Process pending audio buffers on main thread
			while (_pendingBuffers.TryDequeue(out var buffer)) {
				ProcessAudioBuffer(buffer.samples, buffer.channels, buffer.frequency);
			}
		}

		private void HandleSeek() {
			_seekRequested = true;
		}

		private void HandleAudioBuffer(ICollection<float> samples, int channels, int frequency) {
			// Copy samples to array and queue for main thread processing
			var sampleArray = samples is float[] arr ? arr : new List<float>(samples).ToArray();
			var copy = new float[sampleArray.Length];
			System.Array.Copy(sampleArray, copy, sampleArray.Length);
			_pendingBuffers.Enqueue((copy, channels, frequency));
		}

		private void ProcessAudioBuffer(float[] sampleArray, int channels, int frequency) {
			EnsureAudioClip(channels, frequency);
			
			if (_audioClip == null) return;

			var sampleCount = sampleArray.Length;

			// Write samples to the circular buffer
			var clipSamples = _audioClip.samples;
			var writePos = _writePosition % clipSamples;
			
			if (writePos + sampleCount <= clipSamples) {
				_audioClip.SetData(sampleArray, writePos);
			} else {
				// Handle wrap-around
				var firstPartLength = clipSamples - writePos;
				var firstPart = new float[firstPartLength];
				var secondPart = new float[sampleCount - firstPartLength];
				
				System.Array.Copy(sampleArray, 0, firstPart, 0, firstPartLength);
				System.Array.Copy(sampleArray, firstPartLength, secondPart, 0, secondPart.Length);
				
				_audioClip.SetData(firstPart, writePos);
				_audioClip.SetData(secondPart, 0);
			}

			_writePosition += sampleCount;

			// Start playing if not already playing
			if (!audioSource.isPlaying) {
				audioSource.Play();
			}
		}

		private void EnsureAudioClip(int channels, int frequency) {
			if (_audioClip != null && _channels == channels && _frequency == frequency)
				return;

			if (_audioClip != null) {
				audioSource.Stop();
				Destroy(_audioClip);
			}

			_channels = channels;
			_frequency = frequency;
			_writePosition = 0;

			var bufferSamples = frequency * BufferLengthSeconds;
			_audioClip = AudioClip.Create(
				"FFPlayBuffer",
				bufferSamples,
				channels,
				frequency,
				false
			);

			audioSource.clip = _audioClip;
		}

		private void HandlePause() {
			_pauseRequested = true;
		}

		private void HandleResume() {
			_resumeRequested = true;
		}

		private void HandleVolumeChange(float volume) {
			_pendingVolume = volume;
		}
	}
}