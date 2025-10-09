using System;
using FFmpeg.AutoGen;
using UnityEngine;

namespace Hactazia.VideoPlayer.Core {
	/// <summary>
	/// Handles decoding of a single stream (video or audio) from an <see cref="Context"/>.
	/// </summary>
	internal sealed unsafe class Decoder : IDisposable {
		private readonly Context       _ctx;
		private readonly AVCodecContext* _codecContext;
		private readonly AVFrame*        _frame;
		private readonly AVFrame*        _receivedFrame;
		private readonly AVBufferRef*    _hardwareDeviceCtx;

		private const int AvCodecHwConfigMethodHwDeviceCtx = 0x01;

		private bool _disposed;

		public int            StreamIndex         { get; }
		public AVPixelFormat  HardwarePixelFormat { get; } = AVPixelFormat.AV_PIX_FMT_NONE;
		public AVPixelFormat  PixelFormat         { get; }
		public int            Width               { get; }
		public int            Height              { get; }
		public int            Channels            { get; }
		public AVSampleFormat SampleFormat        { get; }
		public int            SampleRate          { get; }

		public Decoder(Context ctx, AVMediaType mediaType, AVHWDeviceType hardwareDevice = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE) {
			_ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));

			AVCodec* codec = null;
			StreamIndex = ffmpeg.av_find_best_stream(_ctx.FormatContext, mediaType, -1, -1, &codec, 0)
				.ThrowFFmpegException("av_find_best_stream");

			if (hardwareDevice != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE) {
				for (var i = 0;; i++) {
					var hwConfig = ffmpeg.avcodec_get_hw_config(codec, i);
					if (hwConfig == null) {
						Debug.LogWarning("No compatible hardware decoder found. Falling back to software decoding.");
						hardwareDevice = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE;
						break;
					}

					if ((hwConfig->methods & AvCodecHwConfigMethodHwDeviceCtx) == 0 || hwConfig->device_type != hardwareDevice) {
						continue;
					}

					HardwarePixelFormat = hwConfig->pix_fmt;
					break;
				}
			}

			_codecContext               = ffmpeg.avcodec_alloc_context3(codec);
			_codecContext->thread_count = 0; // Automatic thread selection

			if (hardwareDevice != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE) {
				AVCodecContext_get_format getHardwareFormat = GetHardwareFormat;
				_codecContext->get_format = getHardwareFormat;
			}

			ffmpeg.avcodec_parameters_to_context(_codecContext, _ctx.GetStreamParameters(StreamIndex))
				.ThrowFFmpegException("avcodec_parameters_to_context");

			if (hardwareDevice != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE) {
				fixed (AVBufferRef** hwDevice = &_hardwareDeviceCtx) {
					ffmpeg.av_hwdevice_ctx_create(hwDevice, hardwareDevice, null, null, 0)
						.ThrowFFmpegException("av_hwdevice_ctx_create");
				}

				_codecContext->hw_device_ctx = ffmpeg.av_buffer_ref(_hardwareDeviceCtx);
			}

			ffmpeg.avcodec_open2(_codecContext, codec, null).ThrowFFmpegException("avcodec_open2");

			Width        = _codecContext->width;
			Height       = _codecContext->height;
			PixelFormat  = _codecContext->pix_fmt;
			Channels     = _codecContext->ch_layout.nb_channels;
			SampleFormat = _codecContext->sample_fmt;
			SampleRate   = _codecContext->sample_rate;

			_frame         = ffmpeg.av_frame_alloc();
			_receivedFrame = ffmpeg.av_frame_alloc();
		}

		public void Dispose() {
			if (_disposed) return;
			_disposed = true;

			if (_frame != null) {
				var frame = _frame;
				ffmpeg.av_frame_free(&frame);
			}

			if (_receivedFrame != null) {
				var received = _receivedFrame;
				ffmpeg.av_frame_free(&received);
			}

			if (_codecContext != null)
				ffmpeg.avcodec_close(_codecContext);

			if (_hardwareDeviceCtx != null) {
				var ctx = _hardwareDeviceCtx;
				ffmpeg.av_buffer_unref(&ctx);
			}
		}

		public bool TryDecode(out AVFrame frame) {
			if (!CanDecode()) {
				frame = default;
				return false;
			}

			ffmpeg.av_frame_unref(_frame);
			ffmpeg.av_frame_unref(_receivedFrame);

			int sendError;
			int receiveError;

			do {
				sendError    = ffmpeg.avcodec_send_packet(_codecContext, _ctx.CurrentPacket);
				receiveError = ffmpeg.avcodec_receive_frame(_codecContext, _frame);
			} while (sendError == ffmpeg.AVERROR(ffmpeg.EAGAIN));

			sendError.ThrowFFmpegException("avcodec_send_packet");

			if (receiveError == ffmpeg.AVERROR(ffmpeg.EAGAIN)) {
				frame = default;
				return false;
			}

			receiveError.ThrowFFmpegException("avcodec_receive_frame");

			frame = _codecContext->hw_device_ctx != null ? TransferFromHardware() : *_frame;
			return frame.format != -1;
		}

		public int DecodeNonAlloc(ref AVFrame target) {
			if (!CanDecode()) return -1;

			ffmpeg.av_frame_unref(_frame);
			ffmpeg.av_frame_unref(_receivedFrame);

			int sendError;
			int receiveError;

			do {
				sendError    = ffmpeg.avcodec_send_packet(_codecContext, _ctx.CurrentPacket);
				receiveError = ffmpeg.avcodec_receive_frame(_codecContext, _frame);
			} while (sendError == ffmpeg.AVERROR(ffmpeg.EAGAIN));

			sendError.ThrowFFmpegException("avcodec_send_packet");

			if (receiveError == ffmpeg.AVERROR(ffmpeg.EAGAIN))
				return -1;

			receiveError.ThrowFFmpegException("avcodec_receive_frame");

			if (_codecContext->hw_device_ctx != null) {
				_receivedFrame->hw_frames_ctx = _codecContext->hw_device_ctx;
				ffmpeg.av_hwframe_transfer_data(_receivedFrame, _frame, 0).ThrowFFmpegException("av_hwframe_transfer_data");
				target           = *_receivedFrame;
				target.pts       = _frame->pts;
				target.duration  = _frame->duration;
				target.time_base = _frame->time_base;
			} else target = *_frame;

			return 0;
		}

		public void Flush() {
			ffmpeg.avcodec_flush_buffers(_codecContext);
		}

		public double GetDuration(AVFrame frame) {
			var timeBase = _ctx.GetStreamTimeBase(StreamIndex);
			var seconds  = ffmpeg.av_q2d(timeBase);
			return frame.duration * seconds;
		}

		public double GetTime(AVFrame frame) {
			var timeBase = _ctx.GetStreamTimeBase(StreamIndex);
			var seconds  = ffmpeg.av_q2d(timeBase);
			return frame.pts * seconds;
		}

		private bool CanDecode() {
			if (_ctx.EndReached) return false;
			var packetIndex = _ctx.PacketStreamIndex;
			return packetIndex == StreamIndex;
		}

		private AVFrame TransferFromHardware() {
			_receivedFrame->hw_frames_ctx = _codecContext->hw_device_ctx;
			ffmpeg.av_hwframe_transfer_data(_receivedFrame, _frame, 0)
				.ThrowFFmpegException("av_hwframe_transfer_data");

			var managed = *_receivedFrame;
			managed.pts       = _frame->pts;
			managed.duration  = _frame->duration;
			managed.time_base = _frame->time_base;
			return managed;
		}

		private AVPixelFormat GetHardwareFormat(AVCodecContext* context, AVPixelFormat* formats) {
			for (var p = formats; *p != AVPixelFormat.AV_PIX_FMT_NONE; p++)
				if (*p == HardwarePixelFormat)
					return *p;
			return AVPixelFormat.AV_PIX_FMT_NONE;
		}
	}
}