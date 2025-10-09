using System;
using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace Hactazia.VideoPlayer.Core {
	internal sealed unsafe class Context : IDisposable {
		private const int SeekSet = 0;
		private const int SeekCur = 1;
		private const int SeekEnd = 2;

		private readonly AVIOContext*            _ioContext;
		private readonly avio_alloc_context_seek _seekCallback;
		private          GCHandle                _streamHandle;
		private          byte*                   _bufferPtr;
		private          bool                    _disposed;

		public bool EndReached { get; private set; }
		public bool IsValid    { get; private set; }

		public Context(Stream stream, uint bufferSize = 16_000_000) {
			if (stream == null) return;

			_bufferPtr = (byte*)ffmpeg.av_malloc(bufferSize);

			avio_alloc_context_read_packet readCallback = ReadPacketCallback;
			if (stream.CanSeek) _seekCallback           = SeekPacketCallback;

			_streamHandle = GCHandle.Alloc(stream, GCHandleType.Normal);
			_ioContext    = ffmpeg.avio_alloc_context(_bufferPtr, (int)bufferSize, 0, GCHandle.ToIntPtr(_streamHandle).ToPointer(), readCallback, null, _seekCallback);

			FormatContext                       =  ffmpeg.avformat_alloc_context();
			FormatContext->flags                |= ffmpeg.AVFMT_FLAG_SHORTEST;
			FormatContext->max_interleave_delta =  100_000_000;
			FormatContext->pb                   =  _ioContext;
			FormatContext->flags                |= ffmpeg.AVFMT_FLAG_CUSTOM_IO | ffmpeg.AVIO_FLAG_NONBLOCK;

			var          contextCopy = FormatContext;
			const string dummyUrl    = "generated.stream";
			ffmpeg.avformat_open_input(&contextCopy, dummyUrl, null, null).ThrowFFmpegException("avformat_open_input");
			ffmpeg.avformat_find_stream_info(FormatContext, null).ThrowFFmpegException("avformat_find_stream_info");

			CurrentPacket = ffmpeg.av_packet_alloc();
			IsValid = true;
		}

		public Context(string url) {
			if (string.IsNullOrWhiteSpace(url)) return;

			FormatContext                       =  ffmpeg.avformat_alloc_context();
			FormatContext->flags                |= ffmpeg.AVFMT_FLAG_SHORTEST;
			FormatContext->max_interleave_delta =  100_000_000;
			FormatContext->avio_flags           =  ffmpeg.AVIO_FLAG_READ | ffmpeg.AVIO_FLAG_NONBLOCK;

			var contextCopy = FormatContext;
			ffmpeg.avformat_open_input(&contextCopy, url, null, null).ThrowFFmpegException("avformat_open_input");
			ffmpeg.avformat_find_stream_info(FormatContext, null).ThrowFFmpegException("avformat_find_stream_info");

			CurrentPacket = ffmpeg.av_packet_alloc();
			IsValid = true;
		}

		public bool HasStream(AVMediaType type) {
			if (!IsValid) return false;
			return ffmpeg.av_find_best_stream(FormatContext, type, -1, -1, null, 0) >= 0;
		}

		public double GetLength(Decoder decoder) {
			if (!IsValid) return 0d;

			var streamIndex = decoder.StreamIndex;
			if (streamIndex < 0 || streamIndex >= FormatContext->nb_streams)
				return 0d;

			var timeBase = FormatContext->streams[streamIndex]->time_base;
			var duration = FormatContext->streams[streamIndex]->duration;
			var seconds  = ffmpeg.av_q2d(timeBase);
			return duration * seconds;
		}

		public bool TryGetTimeBase(AVMediaType type, out AVRational timebase) {
			timebase = default;
			if (!IsValid) return false;

			var streamIndex = ffmpeg.av_find_best_stream(FormatContext, type, -1, -1, null, 0);
			if (streamIndex < 0 || streamIndex >= FormatContext->nb_streams) return false;

			timebase = FormatContext->streams[streamIndex]->time_base;
			return true;
		}

		public bool NextFrame(out AVPacket packet) {
			if (!IsValid) {
				packet = default;
				return false;
			}

			int error;
			do {
				ffmpeg.av_packet_unref(CurrentPacket);
				error = ffmpeg.av_read_frame(FormatContext, CurrentPacket);
				if (error == ffmpeg.AVERROR_EOF) {
					EndReached = true;
					packet     = default;
					return false;
				}

				error.ThrowFFmpegException("av_read_frame");
			} while (error == ffmpeg.AVERROR(ffmpeg.EAGAIN));

			packet     = *CurrentPacket;
			EndReached = false;
			return true;
		}

		public void Seek(Decoder decoder, double offsetSeconds) {
			if (!IsValid) return;
			var streamIndex = decoder.StreamIndex;
			var timeBase    = FormatContext->streams[streamIndex]->time_base;
			var target      = ffmpeg.av_rescale((long)(offsetSeconds * 1000d), timeBase.den, timeBase.num) / 1000;
			ffmpeg.av_seek_frame(FormatContext, streamIndex, Math.Max(0, target), ffmpeg.AVSEEK_FLAG_BACKWARD).ThrowFFmpegException("av_seek_frame");
			EndReached = false;
		}

		public void Dispose() {
			if (_disposed) return;

			_disposed = true;
			IsValid   = false;

			var packet = CurrentPacket;
			if (packet != null) ffmpeg.av_packet_free(&packet);

			var format = FormatContext;
			if (format != null) ffmpeg.avformat_close_input(&format);

			var io = _ioContext;
			if (io != null) ffmpeg.avio_context_free(&io);

			if (_streamHandle.IsAllocated)
				_streamHandle.Free();

			if (_bufferPtr == null) return;

			ffmpeg.av_free(_bufferPtr);
			_bufferPtr = null;
		}

		internal int PacketStreamIndex
			=> CurrentPacket != null ? CurrentPacket->stream_index : -1;

		internal AVPacket* CurrentPacket { get; }
		internal AVFormatContext* FormatContext { get; }

		internal AVCodecParameters* GetStreamParameters(int streamIndex) {
			if (FormatContext == null || streamIndex < 0 || streamIndex >= FormatContext->nb_streams)
				return null;

			return FormatContext->streams[streamIndex]->codecpar;
		}

		internal AVRational GetStreamTimeBase(int streamIndex) {
			if (FormatContext == null || streamIndex < 0 || streamIndex >= FormatContext->nb_streams)
				return default;

			return FormatContext->streams[streamIndex]->time_base;
		}

		private static int ReadPacketCallback(void* opaque, byte* buffer, int bufferSize) {
			if (buffer == null) return ffmpeg.AVERROR_EOF;

			var handle = GCHandle.FromIntPtr((IntPtr)opaque);
			if (!handle.IsAllocated) return ffmpeg.AVERROR_EOF;

			if (handle.Target is not Stream { CanRead: true } stream)
				return ffmpeg.AVERROR_EOF;

			var span = new Span<byte>(buffer, bufferSize);
			var read = stream.Read(span);
			return read == 0 ? ffmpeg.AVERROR_EOF : read;
		}

		private static long SeekPacketCallback(void* opaque, long offset, int whence) {
			var handle = GCHandle.FromIntPtr((IntPtr)opaque);
			if (!handle.IsAllocated) return ffmpeg.AVERROR_EOF;

			if (handle.Target is not Stream { CanSeek: true } stream)
				return ffmpeg.AVERROR_EOF;

			var origin = whence switch {
				ffmpeg.AVSEEK_SIZE => SeekOrigin.Current,
				SeekSet            => SeekOrigin.Begin,
				SeekCur            => SeekOrigin.Current,
				SeekEnd            => SeekOrigin.End,
				_                  => SeekOrigin.Begin
			};

			return stream.Seek(offset, origin);
		}
	}
}