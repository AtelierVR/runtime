using System;
using System.Collections.Generic;
using System.IO;
using FFmpeg.AutoGen;
using Hactazia.FFPlay.Helpers;
using UnityEngine;

namespace Hactazia.FFPlay {
	public class Timings : IDisposable {
		private readonly Context _context;
		public           Decoder Decoder;

		public readonly bool   IsInputValid;
		public          double StartTime;

		private long _pts;

		private AVRational _timeBase;

		public double TimeBaseSeconds { get; private set; }

		private AVPacket _currentPacket;
		private AVFrame  _currentFrame;

		public Timings(Context context, AVMediaType mediaType, AVHWDeviceType deviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE) {
			_context     = context;
			IsInputValid = _context.HasStream(mediaType);
			Init(mediaType, deviceType);
		}

		private void Init(AVMediaType type, AVHWDeviceType deviceType) {
			if (!IsInputValid)
				return;

			if (!_context.TryGetTimeBase(type, out _timeBase))
				return;

			TimeBaseSeconds = ffmpeg.av_q2d(_timeBase);
			Decoder         = new Decoder(_context, type, deviceType);

			// find the start time/time offset
			while (true) {
				if (_context.NextFrame(out var packet)) {
					if (Decoder.Decode(out var frame) != 0) continue;
					_currentPacket = packet;
					_currentFrame  = frame;
					StartTime      = _currentPacket.dts * TimeBaseSeconds;
					break;
				} else break;
			}

			Debug.Log($"timeBase={_timeBase.num}/{_timeBase.den}");
			Debug.Log($"timeBaseSeconds={TimeBaseSeconds}");
		}

		public void Update(double timestamp) {
			if (!IsInputValid)
				return;

			_pts = (long)(Math.Max(double.Epsilon, timestamp) / TimeBaseSeconds);
		}

		public void Seek(double timestamp) {
			if (!IsInputValid)
				return;

			_context.Seek(Decoder, timestamp);
			Decoder.Seek();
			Update(timestamp);
			_currentPacket = default;
		}

		public double GetLength
			=> IsInputValid
				? _context.GetLength(Decoder)
				: 0d;

		public bool IsEndOfFile
			=> IsInputValid && _context.EndReached;

		/// <summary>
		/// Returns the current frame for the active pts, decoding if needed
		/// </summary>
		public AVFrame GetCurrentFrame()
			=> !IsInputValid
				? _error
				: _currentFrame;

		public AVFrame GetFrame(int maxFrames = 250) {
			if (!IsInputValid)
				return _error;

			var i = 0;

			while ((_pts >= _currentPacket.dts || _currentPacket.dts == ffmpeg.AV_NOPTS_VALUE) && i <= maxFrames) {
				i++;
				if (_context.NextFrame(out var packet)) {
					if (Decoder.Decode(out var frame) != 0) continue;
					_currentPacket = packet;
					_currentFrame  = frame;
				} else break;
			}

			return _currentFrame;
		}

		public AVPacket GetPacket(int maxPackets = 250) {
			if (!IsInputValid)
				return default;

			var i = 0;

			while ((_pts >= _currentPacket.dts || _currentPacket.dts == ffmpeg.AV_NOPTS_VALUE) && i <= maxPackets) {
				i++;
				if (_context.NextFrame(out var packet)) {
					_currentPacket = packet;
				} else break;
			}

			return _currentPacket;
		}


		private readonly AVFrame _empty = default;
		private readonly AVFrame _error = new() { format = -1 };

		public int GetFramesNonAlloc(double maxDelta, ref AVFrame[] frames) {
			if (!IsInputValid)
				return 0;

			var ptsDelta  = (long)(Math.Max(double.Epsilon, maxDelta) / TimeBaseSeconds);
			int i         = 0, j = 0;
			var dts       = _currentPacket.dts;
			var maxFrames = frames.Length;

			while ((_pts >= dts || _currentPacket.dts == ffmpeg.AV_NOPTS_VALUE) && i <= maxFrames) {
				i++;
				var frame = frames[j];
				
				if (_context.NextFrame(out var packet)) {
					if (Decoder.DecodeNonAlloc(ref frame) != 0) continue;
					_currentPacket = packet;
					_currentFrame  = frame;
					if (Math.Abs(_pts - packet.dts) > ptsDelta) continue;
					dts         = _currentPacket.dts;
					frames[j++] = frame;
				} else break;
			}

			var capturedFrames = j;
			while (j < frames.Length)
				frames[j++] = _empty;
			return capturedFrames;
		}

		public List<AVFrame> GetFrames(double maxDelta, int maxFrames = 250) {
			if (!IsInputValid)
				return new List<AVFrame>();

			var ptsDelta = (long)(Math.Max(double.Epsilon, maxDelta) / TimeBaseSeconds);
			var frames   = new List<AVFrame>();
			var i        = 0;
			var dts      = _currentPacket.dts;

			while ((_pts >= dts || _currentPacket.dts == ffmpeg.AV_NOPTS_VALUE) && i <= maxFrames) {
				i++;
				if (_context.NextFrame(out var packet)) {
					if (Decoder.Decode(out var frame) != 0) continue;
					_currentPacket = packet;
					_currentFrame  = frame;
					if (Math.Abs(_pts - packet.dts) > ptsDelta) continue;
					dts = _currentPacket.dts;
					frames.Add(frame);
				} else break;
			}

			return frames;
		}

		private AVFrame DecodeFrame() {
			Decoder.Decode(out var frame);
			return frame;
		}

		private AVFrame DecodeMultiFrame() {
			int     retCode;
			AVFrame frame;
			do {
				retCode = Decoder.Decode(out frame);
			} while (retCode == 1);

			return frame;
		}

		public void Dispose() {
			Decoder?.Dispose();
			_context?.Dispose();
		}
	}
}