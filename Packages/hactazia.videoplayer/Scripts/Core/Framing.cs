using System;
using System.IO;
using FFmpeg.AutoGen;

namespace Hactazia.VideoPlayer.Core {
	internal sealed class Framing : IDisposable {
		public Context          Context      { get; }
		public Decoder Decoder      { get; private set; }
		public bool               IsInputValid { get; private set; }
		public double             StartTime    { get; private set; }

	private readonly AVMediaType    _mediaType;
	private readonly AVHWDeviceType _deviceType;
	private          AVPacket       _currentPacket;
	private          AVFrame        _currentFrame;
	private          AVRational     _timeBase;
	private          double         _timeBaseSeconds;
	private          long           _pts;
	private          bool           _disposed;
	
	// PTS discontinuity tracking
	private          long           _lastDts = ffmpeg.AV_NOPTS_VALUE;
	private          long           _ptsOffset;
	private          bool           _discontinuityDetected;		
	
	public Framing(string url, AVMediaType mediaType, AVHWDeviceType deviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE) {
			_mediaType  = mediaType;
			_deviceType = deviceType;
			Context     = new Context(url);
			Initialize();
		}

		public Framing(Stream stream, AVMediaType mediaType, AVHWDeviceType deviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE) {
			_mediaType  = mediaType;
			_deviceType = deviceType;
			Context     = new Context(stream);
			Initialize();
		}

		private void Initialize() {
			if (!Context.IsValid) {
				IsInputValid = false;
				return;
			}

			IsInputValid = Context.HasStream(_mediaType);
			if (!IsInputValid) return;
			
			if (!Context.TryGetTimeBase(_mediaType, out _timeBase)) {
				IsInputValid = false;
				return;
			}

			_timeBaseSeconds = ffmpeg.av_q2d(_timeBase);
			Decoder          = new Decoder(Context, _mediaType, _deviceType);

			// Prime decoder to determine start time
			while (Context.NextFrame(out var packet)) {
				_currentPacket = packet;
				if (!Decoder.TryDecode(out var frame)) continue;
				_currentFrame = frame;
				StartTime     = packet.dts != ffmpeg.AV_NOPTS_VALUE ? packet.dts * _timeBaseSeconds : 0d;
				break;
			}
		}

		public void Update(double timestamp) {
			if (!IsInputValid) return;
			timestamp = Math.Max(double.Epsilon, timestamp);
			_pts      = (long)(timestamp / _timeBaseSeconds);
		}

	public void Seek(double timestamp) {
		if (!IsInputValid) return;
		Context.Seek(Decoder, timestamp);
		Decoder.Flush();
		Update(timestamp);
		_currentPacket = default;
		
		// Reset discontinuity tracking
		_lastDts = ffmpeg.AV_NOPTS_VALUE;
		_ptsOffset = 0;
		_discontinuityDetected = false;
	}		public double GetLength()
			=> IsInputValid
				? Context.GetLength(Decoder)
				: 0d;


		public bool IsEndOfFile()
			=> IsInputValid && Context.EndReached;

		public AVFrame GetFrame(int maxIterations = 250) {
			if (!IsInputValid) return default;

			var iterations = 0;
			while ((NeedMoreData() || _currentPacket.dts == ffmpeg.AV_NOPTS_VALUE) && iterations++ <= maxIterations)
				if (!TryReadNextFrame())
					break;

			return _currentFrame;
		}

		public int GetFramesNonAlloc(double maxDeltaSeconds, AVFrame[] frames) {
			if (!IsInputValid || frames == null) return 0;

			var ptsDelta   = (long)(Math.Max(double.Epsilon, maxDeltaSeconds) / _timeBaseSeconds);
			var captured   = 0;
			var iterations = 0;
			var maxFrames  = frames.Length;

			while ((NeedMoreData() || _currentPacket.dts == ffmpeg.AV_NOPTS_VALUE) && iterations++ <= maxFrames) {
				if (!TryReadNextFrame()) break;
				if (Math.Abs(_pts - _currentPacket.dts) > ptsDelta) continue;
				frames[captured++] = _currentFrame;
				if (captured >= maxFrames) break;
			}

			for (var i = captured; i < frames.Length; i++)
				frames[i] = default;

			return captured;
		}


	private void HandleDiscontinuity(AVPacket packet) {
		if (packet.dts == ffmpeg.AV_NOPTS_VALUE) return;
		
		if (_lastDts != ffmpeg.AV_NOPTS_VALUE) {
			// Detect significant jump in DTS (more than 2 seconds)
			var dtsDelta = Math.Abs(packet.dts - _lastDts);
			var deltaSeconds = dtsDelta * _timeBaseSeconds;
			
			if (deltaSeconds > 2.0) {
				_discontinuityDetected = true;
				_ptsOffset += (packet.dts - _lastDts);
				UnityEngine.Debug.LogWarning($"[VideoPlayer] Discontinuity detected: {deltaSeconds:F2}s jump. Adjusting PTS offset.");
			}
		}
		
		_lastDts = packet.dts;
	}
	
	private long GetAdjustedDts(AVPacket packet) {
		if (packet.dts == ffmpeg.AV_NOPTS_VALUE) return ffmpeg.AV_NOPTS_VALUE;
		return _discontinuityDetected ? packet.dts - _ptsOffset : packet.dts;
	}
	
	private bool NeedMoreData() {
		if (_currentPacket.dts == ffmpeg.AV_NOPTS_VALUE)
			return true;
		var adjustedDts = GetAdjustedDts(_currentPacket);
		return _pts >= adjustedDts;
	}	private bool TryReadNextFrame() {
		while (Context.NextFrame(out var packet)) {
			HandleDiscontinuity(packet);
			_currentPacket = packet;
			if (!Decoder.TryDecode(out var frame))
				continue;
			_currentFrame = frame;
			return true;
		}

		return false;
	}		public void Dispose() {
			if (_disposed)
				return;

			_disposed = true;
			Decoder?.Dispose();
			Context?.Dispose();
		}
	}
}