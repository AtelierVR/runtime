using System;
using System.Collections.Generic;
using System.Linq;
using FFmpeg.AutoGen;
using UnityEngine;
using UnityEngine.Events;

namespace Hactazia.FFPlay.Core {
	public struct MediaPart {
		public StreamDecoder  Decoder;
		public AVFrame        Current;
		public Queue<AVFrame> Pending;
		public int            MaxPending;
		public long           Pts;
		public AVPacket       CurrentPacket;

		// Discontinuity tracking per stream
		public double LastPts;
		public double DiscontinuityOffset;

		public bool IsValid
			=> Decoder?.IsValid ?? false;

		public double TimeBase
			=> Decoder?.TimeBase ?? 1d;

		public void Flush() {
			Decoder?.Flush();
			Current       = default;
			CurrentPacket = default;
			Pending?.Clear();
		}

		public void ResetDiscontinuity() {
			LastPts             = 0;
			DiscontinuityOffset = 0;
		}
	}


	/// <summary>
	/// Unified timing system that handles multiple stream types (video, audio, subtitle)
	/// from a single MediaSource with a single demuxing thread.
	/// Supports PTS discontinuity detection, correction, and stall detection.
	/// </summary>
	public sealed class MultiStreamTimings : IStreamTimings {
		private readonly MediaSource _source;

		private readonly Dictionary<MediaType, MediaPart> _parts = new();

		private object Lock { get; } = new();

		private AVPacket _current;

		private static readonly AVFrame ErrorFrame = new() { format = -1 };

		#region Discontinuity Properties

		/// <summary>
		/// Event fired when a PTS discontinuity is detected.
		/// Parameters: MediaType, previousPts, currentPts, delta
		/// </summary>
		public UnityEvent<MediaType, double, double, double> OnDiscontinuity { get; } = new();

		/// <summary>
		/// Threshold in seconds for detecting a discontinuity. Default is 1.0 second.
		/// </summary>
		public double DiscontinuityThreshold { get; set; } = 1.0;

		/// <summary>
		/// Whether to automatically correct PTS values when discontinuity is detected.
		/// </summary>
		public bool DiscontinuityCorrectionEnabled { get; set; } = true;

		/// <summary>
		/// Gets the current discontinuity offset for a specific stream type.
		/// </summary>
		public double GetDiscontinuityOffset(MediaType type)
			=> Has(type) ? _parts[type].DiscontinuityOffset : 0;

		#endregion


		public MultiStreamTimings(MediaSource source, AVHWDeviceType hwDevice = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE) {
			_source = source ?? throw new ArgumentNullException(nameof(source));

			if (_source.HasStream(AVMediaType.AVMEDIA_TYPE_VIDEO))
				_parts[MediaType.Video] = new MediaPart {
					Decoder       = new StreamDecoder(_source, AVMediaType.AVMEDIA_TYPE_VIDEO, hwDevice),
					Current       = default,
					CurrentPacket = default,
					Pending       = new Queue<AVFrame>(),
					MaxPending    = 100,
					Pts           = 0
				};

			if (_source.HasStream(AVMediaType.AVMEDIA_TYPE_AUDIO))
				_parts[MediaType.Audio] = new MediaPart {
					Decoder       = new StreamDecoder(_source, AVMediaType.AVMEDIA_TYPE_AUDIO),
					Current       = default,
					CurrentPacket = default,
					Pending       = new Queue<AVFrame>(),
					MaxPending    = 500,
					Pts           = 0
				};

			if (_source.HasStream(AVMediaType.AVMEDIA_TYPE_SUBTITLE))
				_parts[MediaType.Subtitle] = new MediaPart {
					Decoder       = new StreamDecoder(_source, AVMediaType.AVMEDIA_TYPE_SUBTITLE),
					Current       = default,
					CurrentPacket = default,
					Pending       = new Queue<AVFrame>(),
					MaxPending    = 100,
					Pts           = 0
				};

			// Find start time from first frame
			FindStartTime();

			_source.OnLog.AddListener(OnLogMessage);
		}

		private void FindStartTime() {
			if (!IsValid) return;

			while (_source.ReadPacket(out var packet)) {
				_current = packet;

				foreach (var (type, part) in _parts) {
					if (!part.IsValid || !part.Decoder.CanDecode(packet)) continue;

					if (part.Decoder.Decode(packet, out var frame)) {
						var p = _parts[type];
						p.Current       = frame;
						p.CurrentPacket = packet;
						_parts[type]    = p;

						StartTime = part.Decoder.GetFrameTime(frame);
						return;
					}
				}
			}
		}


		public bool IsValid
			=> _source?.IsValid ?? false;

		public bool IsEnd
			=> _source?.EndReached ?? true;

		public double StartTime { get; private set; } = 0d;

		public bool Has(MediaType type)
			=> _parts.ContainsKey(type) && _parts[type].IsValid;

		public StreamDecoder Get(MediaType type)
			=> Has(type) ? _parts[type].Decoder : null;

		public void Update(MediaType type, double time) {
			if (!Has(type)) return;

			var part = _parts[type];
			// Apply discontinuity offset when converting time to PTS
			var correctedTime = time - part.DiscontinuityOffset;
			part.Pts     = (long)(Math.Max(double.Epsilon, correctedTime) / part.TimeBase);
			_parts[type] = part;
		}

		#region Discontinuity Detection

		/// <summary>
		/// Checks for PTS discontinuity and applies correction if needed.
		/// </summary>
		private void CheckAndCorrectDiscontinuity(MediaType type, double currentPts, ref MediaPart part) {
			if (!DiscontinuityCorrectionEnabled || double.IsNaN(part.LastPts) || part.LastPts == 0) {
				part.LastPts = currentPts;
				return;
			}

			var delta    = currentPts - part.LastPts;
			var absDelta = Math.Abs(delta);

			// Check for discontinuity (large jump forward or backward)
			if (absDelta > DiscontinuityThreshold) {
				var oldOffset = part.DiscontinuityOffset;

				// Adjust offset to maintain continuous playback
				part.DiscontinuityOffset += part.LastPts - currentPts + (delta > 0 ? 0 : part.TimeBase);

				Debug.LogWarning($"[MultiStreamTimings] PTS discontinuity detected in {type}: " + $"jump from {part.LastPts:F3}s to {currentPts:F3}s (delta: {delta:F3}s), " + $"offset adjusted from {oldOffset:F3}s to {part.DiscontinuityOffset:F3}s");

				OnDiscontinuity.Invoke(type, part.LastPts, currentPts, delta);
			}

			part.LastPts = currentPts;
		}

		/// <summary>
		/// Resets all discontinuity offsets to zero.
		/// </summary>
		public void ResetDiscontinuityOffsets() {
			foreach (var type in _parts.Keys.ToList()) {
				var part = _parts[type];
				part.ResetDiscontinuity();
				_parts[type] = part;
			}
		}

		#endregion

		public AVFrame GetFrame(MediaType type, double maxDelta = 250) {
			if (!Has(type))
				return ErrorFrame;

			lock (Lock) {
				var part = _parts[type];

				// First, drain pending queue
				if (part.Pending.Count > 0) {
					part.Current = part.Pending.Dequeue();
					_parts[type] = part;
					return part.Current;
				}

				var i         = 0;
				var maxFrames = (int)maxDelta;

				while ((part.Pts >= part.CurrentPacket.dts || part.CurrentPacket.dts == ffmpeg.AV_NOPTS_VALUE) && i <= maxFrames) {
					i++;

					if (!_source.ReadPacket(out var packet))
						break;

					_current = packet;

					// Queue frames from other streams into their pending queues
					foreach (var otherType in _parts.Keys.ToList()) {
						if (otherType == type) continue;
						var otherPart = _parts[otherType];
						if (!otherPart.IsValid) continue;
						if (!otherPart.Decoder.CanDecode(packet)) continue;

						// Send packet and receive ALL frames (important for audio)
						if (otherPart.Decoder.SendPacket(packet)) {
							while (otherPart.Decoder.ReceiveFrame(out var otherFrame)) {
								// Check for discontinuity on other streams
								var otherPtsSeconds = otherPart.Decoder.GetFrameTime(otherFrame);
								CheckAndCorrectDiscontinuity(otherType, otherPtsSeconds, ref otherPart);

								if (otherPart.Pending.Count >= otherPart.MaxPending) {
									// Drop oldest frame to make room for new one (FIFO replacement)
									var dropped = otherPart.Pending.Dequeue();
									Debug.LogWarning($"[MultiStreamTimings] Frame dropped for {otherType}: pending queue full (max: {otherPart.MaxPending}), dropped PTS: {otherPart.Decoder.GetFrameTime(dropped):F3}s");
								}
								otherPart.Pending.Enqueue(otherFrame);
							}
							_parts[otherType] = otherPart;
						}
					}

					// Decode for target stream - receive ALL frames and queue extras
					if (!part.Decoder.CanDecode(packet)) continue;

					if (part.Decoder.SendPacket(packet)) {
						bool firstFrame = true;
						while (part.Decoder.ReceiveFrame(out var frame)) {
							// Check for discontinuity
							var framePtsSeconds = part.Decoder.GetFrameTime(frame);
							CheckAndCorrectDiscontinuity(type, framePtsSeconds, ref part);

							part.CurrentPacket = packet;

							if (firstFrame) {
								part.Current = frame;
								firstFrame   = false;
							} else {
								// Queue additional frames for later
								if (part.Pending.Count < part.MaxPending)
									part.Pending.Enqueue(frame);
							}
						}
						_parts[type] = part;
					}
				}

				return part.Current;
			}
		}

		public int GetFrames(MediaType type, ref AVFrame[] frames, double maxDelta = 250) {
			if (!Has(type))
				return 0;

			lock (Lock) {
				var part      = _parts[type];
				var ptsDelta  = (long)(Math.Max(double.Epsilon, maxDelta) / part.TimeBase);
				int i         = 0, j = 0;
				var dts       = part.CurrentPacket.dts;
				var maxFrames = frames.Length;

				// First, drain pending queue
				while (part.Pending.Count > 0 && j < maxFrames) {
					var pending = part.Pending.Dequeue();
					part.Current = pending;
					frames[j++]  = pending;
				}

				_parts[type] = part;

				while ((part.Pts >= dts || part.CurrentPacket.dts == ffmpeg.AV_NOPTS_VALUE) && i <= maxFrames) {
					i++;

					if (!_source.ReadPacket(out var packet))
						break;

					_current = packet;

					// Queue frames from other streams into their pending queues
					foreach (var otherType in _parts.Keys.ToList()) {
						if (otherType == type) continue;
						var otherPart = _parts[otherType];
						if (!otherPart.IsValid) continue;
						if (!otherPart.Decoder.CanDecode(packet)) continue;

						// Send packet and receive ALL frames (important for audio)
						if (otherPart.Decoder.SendPacket(packet)) {
							while (otherPart.Decoder.ReceiveFrame(out var otherFrame)) {
								// Check for discontinuity on other streams
								var otherPtsSeconds = otherPart.Decoder.GetFrameTime(otherFrame);
								CheckAndCorrectDiscontinuity(otherType, otherPtsSeconds, ref otherPart);

								if (otherPart.Pending.Count >= otherPart.MaxPending) {
									// Drop oldest frame to make room for new one (FIFO replacement)
									var dropped = otherPart.Pending.Dequeue();
									Debug.LogWarning($"[MultiStreamTimings] Frame dropped for {otherType}: pending queue full (max: {otherPart.MaxPending}), dropped PTS: {otherPart.Decoder.GetFrameTime(dropped):F3}s");
								}
								otherPart.Pending.Enqueue(otherFrame);
							}
							_parts[otherType] = otherPart;
						}
					}

					// Decode for target stream - receive ALL frames
					if (!part.Decoder.CanDecode(packet)) continue;

					if (part.Decoder.SendPacket(packet)) {
						while (part.Decoder.ReceiveFrame(out var frame)) {
							// Check for discontinuity
							var framePtsSeconds = part.Decoder.GetFrameTime(frame);
							CheckAndCorrectDiscontinuity(type, framePtsSeconds, ref part);

							part.CurrentPacket = packet;
							part.Current       = frame;

							// Filter by ptsDelta
							if (Math.Abs(part.Pts - packet.dts) > ptsDelta) continue;

							dts = packet.dts;
							if (j < maxFrames)
								frames[j++] = frame;
						}
					}
				}

				_parts[type] = part;

				// Clear remaining slots
				var capturedFrames = j;
				while (j < frames.Length)
					frames[j++] = default;

				return capturedFrames;
			}
		}

		public int GetPendingSize(MediaType type)
			=> Has(type) ? _parts[type].Pending.Count : 0;

		public void Seek(double timestamp) {
			if (!IsValid) return;

			lock (Lock) {
				// Reset discontinuity tracking (seek is an intentional discontinuity)
				ResetDiscontinuityOffsets();

				foreach (var (k, v) in _parts) {
					if (!Has(k)) continue;
					_source.Seek(v.Decoder.StreamIndex, timestamp);
					break;
				}

				_current = default;
				foreach (var type in _parts.Keys.ToList()) {
					var part = _parts[type];
					part.Flush();
					_parts[type] = part;
				}
			}
		}

		public double Length {
			get {
				lock (Lock) {
					var keys      = _parts.Keys.ToList();
					var firstType = keys.FirstOrDefault(k => Has(k));
					if (firstType == default && !Has(firstType)) return 0;
					return _source.GetDuration(_parts[firstType].Decoder.StreamIndex);
				}
			}
		}

		private void OnLogMessage(int level, string message) {
			// Forward logs if needed
		}

		public void Dispose() {
			lock (Lock) {
				_source?.OnLog.RemoveListener(OnLogMessage);

				foreach (var type in _parts.Keys.ToList()) {
					var part = _parts[type];
					part.Decoder?.Dispose();
					part.Pending?.Clear();
				}

				_parts.Clear();
			}

			_source?.Dispose();
		}
	}
}