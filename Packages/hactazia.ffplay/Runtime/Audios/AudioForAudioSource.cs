using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Hactazia.FFPlay {
	[RequireComponent(typeof(AudioSource))]
	public class AudioForAudioSource : MonoBehaviour {
		public AudioWorker worker;

		[HideInInspector]
		public AudioSource audioSource;

		private AudioClip _audioClip;
		private int _writePosition;
		private int _readPosition;
		private int _channels;
		private int _frequency;
		private const int BufferLengthSeconds = 10;
		private const int MinBufferSamples = 4096; // Minimum samples before starting playback
		private const int FadeSamples = 64; // Samples for crossfade to avoid clicks
		
		private readonly ConcurrentQueue<(float[] samples, int channels, int frequency)> _pendingBuffers = new();
		private volatile bool _seekRequested;
		private volatile bool _pauseRequested;
		private volatile bool _resumeRequested;
		private volatile float _pendingVolume = -1f;
		
		private float _lastSample; // For continuity between buffers
		private bool _isBuffering = true;
		private int _bufferedSamples;
		private float[] _circularBuffer;

		public void Awake() {
			worker ??= GetComponentInParent<AudioWorker>();

			if (!worker) {
				Debug.LogError($"[{nameof(AudioForAudioSource)}] No AudioWorker found in parent hierarchy.", this);
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
				ResetBuffer();
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
				if (audioSource != null && !audioSource.isPlaying && _audioClip != null && !_isBuffering) {
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
			
			// Check for buffer underrun
			if (audioSource != null && audioSource.isPlaying && _audioClip != null) {
				var playPosition = audioSource.timeSamples;
				var availableSamples = GetAvailableSamples(playPosition);
				
				if (availableSamples < MinBufferSamples / 2) {
					// Buffer underrun - pause and wait for more data
					audioSource.Pause();
					_isBuffering = true;
				}
			}
		}

		private int GetAvailableSamples(int playPosition) {
			if (_circularBuffer == null) return 0;
			var bufferLength = _circularBuffer.Length;
			var writePos = _writePosition % bufferLength;
			
			if (writePos >= playPosition)
				return writePos - playPosition;
			else
				return bufferLength - playPosition + writePos;
		}

		private void ResetBuffer() {
			_writePosition = 0;
			_readPosition = 0;
			_bufferedSamples = 0;
			_isBuffering = true;
			_lastSample = 0f;
			
			// Clear the circular buffer with silence
			if (_circularBuffer != null) {
				System.Array.Clear(_circularBuffer, 0, _circularBuffer.Length);
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
			
			if (_audioClip == null || _circularBuffer == null) return;

			// Apply fade-in to avoid clicks at the start of new buffers
			ApplyCrossfade(sampleArray);

			var sampleCount = sampleArray.Length;
			var bufferLength = _circularBuffer.Length;
			var writePos = _writePosition % bufferLength;
			
			// Write to circular buffer
			if (writePos + sampleCount <= bufferLength) {
				System.Array.Copy(sampleArray, 0, _circularBuffer, writePos, sampleCount);
			} else {
				// Handle wrap-around
				var firstPartLength = bufferLength - writePos;
				System.Array.Copy(sampleArray, 0, _circularBuffer, writePos, firstPartLength);
				System.Array.Copy(sampleArray, firstPartLength, _circularBuffer, 0, sampleCount - firstPartLength);
			}

			_writePosition += sampleCount;
			_bufferedSamples += sampleCount;

			// Update AudioClip with the new data
			UpdateAudioClip(sampleArray, sampleCount, writePos);

			// Start playing once we have enough buffered
			if (_isBuffering && _bufferedSamples >= MinBufferSamples) {
				_isBuffering = false;
				if (!audioSource.isPlaying) {
					audioSource.Play();
				}
			}
		}

		private void ApplyCrossfade(float[] samples) {
			if (samples.Length == 0) return;
			
			// Fade in from last sample to avoid discontinuity
			var fadeLength = Mathf.Min(FadeSamples, samples.Length);
			for (var i = 0; i < fadeLength; i++) {
				var t = (float)i / fadeLength;
				samples[i] = Mathf.Lerp(_lastSample, samples[i], t);
			}
			
			// Store last sample for next buffer
			_lastSample = samples[samples.Length - 1];
		}

		private void UpdateAudioClip(float[] sampleArray, int sampleCount, int writePos) {
			var clipSamples = _audioClip.samples;
			
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

			var bufferSamples = frequency * BufferLengthSeconds;
			_circularBuffer = new float[bufferSamples];
			
			_audioClip = AudioClip.Create(
				"FFPlayBuffer",
				bufferSamples,
				channels,
				frequency,
				false
			);

			audioSource.clip = _audioClip;
			ResetBuffer();
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