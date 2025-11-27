using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using FFmpeg.AutoGen;
using UnityEngine;
using UnityEngine.Events;

namespace Hactazia.FFPlay.Core {
	/// <summary>
	/// Manages FFmpeg format context for media file/stream access.
	/// Thread-safe wrapper around AVFormatContext.
	/// </summary>
	public sealed unsafe class MediaSource : IDisposable {
		private readonly object           _lock = new();
		private readonly AVFormatContext* _formatContext;
		private readonly AVIOContext*     _ioContext;
		private readonly Stream           _stream;
		private readonly GCHandle         _streamHandle;
		private readonly byte*            _ioBuffer;
		private readonly AVPacket*        _packet;

		// Keep delegates alive to prevent GC
		private readonly avio_alloc_context_read_packet _readCallback;
		private readonly avio_alloc_context_seek        _seekCallback;

		public bool IsValid    { get; private set; }
		public bool EndReached { get; private set; }

		public readonly UnityEvent<int, string> OnLog = new();

		#region Constructors

		public MediaSource(string url) {
			if (string.IsNullOrWhiteSpace(url))
				return;

			_formatContext                       =  ffmpeg.avformat_alloc_context();
			_formatContext->flags                |= ffmpeg.AVFMT_FLAG_SHORTEST;
			_formatContext->max_interleave_delta =  100_000_000;
			_formatContext->avio_flags           =  ffmpeg.AVIO_FLAG_READ | ffmpeg.AVIO_FLAG_NONBLOCK;

			var ctx = _formatContext;
			ffmpeg.avformat_open_input(&ctx, url, null, null).ThrowFFmpegException();
			ffmpeg.avformat_find_stream_info(_formatContext, null).ThrowFFmpegException();

			_packet = ffmpeg.av_packet_alloc();
			IsValid = true;

			Initializer.OnFFmpegLog.AddListener(HandleLog);
		}

		public MediaSource(Stream stream, uint bufferSize = 16_000_000) {
			if (stream == null)
				return;

			_stream       = stream;
			_ioBuffer     = (byte*)ffmpeg.av_malloc(bufferSize);
			_readCallback = ReadCallback;
			_seekCallback = stream.CanSeek ? SeekCallback : null;
			_streamHandle = GCHandle.Alloc(_stream, GCHandleType.Normal);

			_ioContext = ffmpeg.avio_alloc_context(
				_ioBuffer, (int)bufferSize, 0,
				GCHandle.ToIntPtr(_streamHandle).ToPointer(),
				_readCallback, null, _seekCallback
			);

			_formatContext                       =  ffmpeg.avformat_alloc_context();
			_formatContext->flags                |= ffmpeg.AVFMT_FLAG_SHORTEST;
			_formatContext->max_interleave_delta =  100_000_000;
			_formatContext->pb                   =  _ioContext;
			_formatContext->flags                |= ffmpeg.AVFMT_FLAG_CUSTOM_IO;
			_formatContext->avio_flags           =  ffmpeg.AVIO_FLAG_READ | ffmpeg.AVIO_FLAG_NONBLOCK;

			var ctx = _formatContext;
			ffmpeg.avformat_open_input(&ctx, "stream", null, null).ThrowFFmpegException();
			ffmpeg.avformat_find_stream_info(_formatContext, null).ThrowFFmpegException();

			_packet = ffmpeg.av_packet_alloc();
			IsValid = true;

			Initializer.OnFFmpegLog.AddListener(HandleLog);
		}

		#endregion

		#region Stream Info

		public AVFormatContext* FormatContext
			=> _formatContext;

		public bool HasStream(AVMediaType type) {
			if (!IsValid) return false;
			return ffmpeg.av_find_best_stream(_formatContext, type, -1, -1, null, 0) >= 0;
		}

		public int GetStreamIndex(AVMediaType type) {
			if (!IsValid) return -1;
			return ffmpeg.av_find_best_stream(_formatContext, type, -1, -1, null, 0);
		}

		public AVRational GetTimeBase(int streamIndex) {
			if (!IsValid || streamIndex < 0 || streamIndex >= _formatContext->nb_streams)
				return new AVRational { num = 1, den = 1 };
			return _formatContext->streams[streamIndex]->time_base;
		}

		public double GetDuration(int streamIndex) {
			if (!IsValid || streamIndex < 0 || streamIndex >= _formatContext->nb_streams)
				return 0d;

			var stream   = _formatContext->streams[streamIndex];
			var timeBase = ffmpeg.av_q2d(stream->time_base);
			return stream->duration * timeBase;
		}

		public double GetStartTime(int streamIndex) {
			if (!IsValid || streamIndex < 0 || streamIndex >= _formatContext->nb_streams)
				return 0d;

			var stream   = _formatContext->streams[streamIndex];
			var timeBase = ffmpeg.av_q2d(stream->time_base);

			if (stream->start_time == ffmpeg.AV_NOPTS_VALUE)
				return 0d;

			return stream->start_time * timeBase;
		}

		public double GetFrameRate(int streamIndex) {
			if (!IsValid || streamIndex < 0 || streamIndex >= _formatContext->nb_streams)
				return 0d;

			var stream = _formatContext->streams[streamIndex];
			if (stream->avg_frame_rate.den == 0) return 0d;
			return (double)stream->avg_frame_rate.num / stream->avg_frame_rate.den;
		}

		#endregion

		#region Packet Reading

		/// <summary>
		/// Reads the next packet from the media source.
		/// Thread-safe.
		/// </summary>
		public bool ReadPacket(out AVPacket packet) {
			packet = default;
			if (!IsValid) return false;

			lock (_lock) {
				ffmpeg.av_packet_unref(_packet);

				int       error;
				int       retries    = 0;
				const int maxRetries = 3;

				do {
					error = ffmpeg.av_read_frame(_formatContext, _packet);

					if (error == ffmpeg.AVERROR_EOF) {
						EndReached = true;
						return false;
					}

					if (error < 0) {
						var desc = error.FFmpegDescribe();
						if (desc.Contains("tls", StringComparison.OrdinalIgnoreCase) || desc.Contains("Connection", StringComparison.OrdinalIgnoreCase) || desc.Contains("I/O", StringComparison.OrdinalIgnoreCase)) {
							retries++;
							if (retries < maxRetries) {
								Thread.Sleep(100 * retries);
								continue;
							}
						}
					}

					if (error != ffmpeg.AVERROR(ffmpeg.EAGAIN))
						break;
				} while (true);

				if (error < 0) {
					error.ThrowFFmpegException();
					return false;
				}

				packet     = *_packet;
				EndReached = false;
				return true;
			}
		}

		/// <summary>
		/// Seeks to a specific timestamp in a stream.
		/// </summary>
		public void Seek(int streamIndex, double timestamp) {
			if (!IsValid) return;

			lock (_lock) {
				var timeBase = GetTimeBase(streamIndex);
				var frame    = (long)(timestamp / ffmpeg.av_q2d(timeBase));
				ffmpeg.av_seek_frame(_formatContext, streamIndex, Math.Max(0, frame), ffmpeg.AVSEEK_FLAG_BACKWARD)
					.ThrowFFmpegException();
				EndReached = false;
			}
		}

		#endregion

		#region IO Callbacks

		private static int ReadCallback(void* opaque, byte* buf, int bufSize) {
			var handle = GCHandle.FromIntPtr((IntPtr)opaque);
			if (!handle.IsAllocated) return ffmpeg.AVERROR_EOF;

			var stream = (Stream)handle.Target;
			if (stream == null || !stream.CanRead) return ffmpeg.AVERROR_EOF;

			var span  = new Span<byte>(buf, bufSize);
			int count = stream.Read(span);
			return count == 0 ? ffmpeg.AVERROR_EOF : count;
		}

		private static long SeekCallback(void* opaque, long offset, int whence) {
			var handle = GCHandle.FromIntPtr((IntPtr)opaque);
			if (!handle.IsAllocated) return ffmpeg.AVERROR_EOF;

			var stream = (Stream)handle.Target;
			if (stream == null || !stream.CanSeek) return ffmpeg.AVERROR_EOF;

			return stream.Seek(offset, SeekOrigin.Begin);
		}

		#endregion

		#region Logging

		private void HandleLog(FFmpegLogEventArgs args) {
			if (args.Context == null || _formatContext == null) return;
			if (args.Context != _formatContext) return;
			OnLog?.Invoke(args.Level, args.Message);
		}

		#endregion

		#region Disposal

		public void Dispose() {
			if (!IsValid) return;
			IsValid = false;

			Initializer.OnFFmpegLog.RemoveListener(HandleLog);

			var pkt = _packet;
			if (pkt != null) ffmpeg.av_packet_free(&pkt);

			var ctx = _formatContext;
			if (ctx != null) ffmpeg.avformat_close_input(&ctx);

			var io = _ioContext;
			if (io != null) ffmpeg.avio_context_free(&io);

			if (_ioBuffer != null) ffmpeg.av_free(_ioBuffer);

			if (_streamHandle.IsAllocated) _streamHandle.Free();
			_stream?.Dispose();
		}

		#endregion
	}
}