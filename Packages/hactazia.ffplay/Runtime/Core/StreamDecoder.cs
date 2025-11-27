using System;
using System.Drawing;
using FFmpeg.AutoGen;
using UnityEngine;

namespace Hactazia.FFPlay.Core {
	/// <summary>
	/// Decodes a specific stream (video, audio, or subtitle) from a MediaSource.
	/// </summary>
	public sealed unsafe class StreamDecoder : IDisposable {
		private readonly MediaSource               _source;
		private readonly AVCodecContext*           _codecContext;
		private readonly AVFrame*                  _frame;
		private readonly AVFrame*                  _hwFrame;
		private readonly AVBufferRef*              _hwDeviceContext;
		private readonly AVCodecContext_get_format _getHwFormat;

		public int            StreamIndex   { get; }
		public AVMediaType    MediaType     { get; }
		public AVPixelFormat  HwPixelFormat { get; } = AVPixelFormat.AV_PIX_FMT_NONE;
		public Size           FrameSize     { get; }
		public AVPixelFormat  PixelFormat   { get; }
		public int            Channels      { get; }
		public AVSampleFormat SampleFormat  { get; }
		public int            SampleRate    { get; }
		public bool           IsValid       { get; }
		public double         TimeBase      { get; }

		public AVCodecContext* Codec
			=> _codecContext;

		public StreamDecoder(MediaSource source, AVMediaType mediaType, AVHWDeviceType hwDeviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE) {
			_source   = source;
			MediaType = mediaType;

			AVCodec* codec = null;
			StreamIndex = ffmpeg.av_find_best_stream(
				source.FormatContext, mediaType, -1, -1, &codec, 0
			);

			if (StreamIndex < 0) {
				IsValid = false;
				return;
			}

			// Hardware acceleration setup
			if (hwDeviceType != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE) {
				for (int i = 0;; i++) {
					var hwConfig = ffmpeg.avcodec_get_hw_config(codec, i);
					if (hwConfig == null) {
						hwDeviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE;
						break;
					}

					if ((hwConfig->methods & 1) != 0 && hwConfig->device_type == hwDeviceType) {
						HwPixelFormat = hwConfig->pix_fmt;
						Debug.Log($"HW decoder {hwDeviceType} format {HwPixelFormat} selected.");
						break;
					}
				}
			}

			_codecContext               = ffmpeg.avcodec_alloc_context3(codec);
			_codecContext->thread_count = 0;

			if (hwDeviceType != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE) {
				_getHwFormat              = GetHwFormat;
				_codecContext->get_format = _getHwFormat;
			}

			ffmpeg.avcodec_parameters_to_context(
					_codecContext,
					source.FormatContext->streams[StreamIndex]->codecpar
				)
				.ThrowFFmpegException();

			if (hwDeviceType != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE) {
				fixed (AVBufferRef** ctx = &_hwDeviceContext) {
					ffmpeg.av_hwdevice_ctx_create(ctx, hwDeviceType, null, null, 0)
						.ThrowFFmpegException();
				}

				_codecContext->hw_device_ctx = ffmpeg.av_buffer_ref(_hwDeviceContext);
			}

			ffmpeg.avcodec_open2(_codecContext, codec, null).ThrowFFmpegException();

			FrameSize    = new Size(_codecContext->width, _codecContext->height);
			PixelFormat  = _codecContext->pix_fmt;
			Channels     = _codecContext->ch_layout.nb_channels;
			SampleFormat = _codecContext->sample_fmt;
			SampleRate   = _codecContext->sample_rate;

			var tb = source.GetTimeBase(StreamIndex);
			TimeBase = (double)tb.num / tb.den;

			_frame   = ffmpeg.av_frame_alloc();
			_hwFrame = ffmpeg.av_frame_alloc();
			IsValid  = true;
		}

		private AVPixelFormat GetHwFormat(AVCodecContext* ctx, AVPixelFormat* formats) {
			for (var p = (int*)formats; *p != -1; p++)
				if (*p == (int)HwPixelFormat)
					return (AVPixelFormat)(*p);
			return AVPixelFormat.AV_PIX_FMT_NONE;
		}

		/// <summary>
		/// Checks if the given packet belongs to this decoder's stream.
		/// </summary>
		public bool CanDecode(AVPacket packet)
			=> packet.stream_index == StreamIndex;

		/// <summary>
		/// Sends a packet to the decoder. Call ReceiveFrame() afterwards to get decoded frames.
		/// Returns true if the packet was accepted.
		/// </summary>
		public bool SendPacket(AVPacket packet) {
			if (!IsValid || packet.stream_index != StreamIndex)
				return false;

			int error = ffmpeg.avcodec_send_packet(_codecContext, &packet);

			// EAGAIN means decoder buffer is full, need to receive frames first
			if (error == ffmpeg.AVERROR(ffmpeg.EAGAIN))
				return false;

			// Invalid data - skip this packet
			if (error == ffmpeg.AVERROR_INVALIDDATA)
				return false;

			// EOF is acceptable
			if (error != 0 && error != ffmpeg.AVERROR_EOF)
				error.ThrowFFmpegException();

			return true;
		}

		/// <summary>
		/// Receives a decoded frame from the decoder.
		/// Call this repeatedly after SendPacket() until it returns false to get all frames.
		/// </summary>
		public bool ReceiveFrame(out AVFrame frame) {
			frame = default;

			if (!IsValid)
				return false;

			ffmpeg.av_frame_unref(_frame);
			ffmpeg.av_frame_unref(_hwFrame);

			int error = ffmpeg.avcodec_receive_frame(_codecContext, _frame);

			// EAGAIN means need more packets
			if (error == ffmpeg.AVERROR(ffmpeg.EAGAIN))
				return false;

			// Invalid data - skip
			if (error == ffmpeg.AVERROR_INVALIDDATA)
				return false;

			// EOF is acceptable but means no more frames
			if (error == ffmpeg.AVERROR_EOF)
				return false;

			if (error != 0)
				error.ThrowFFmpegException();

			// Handle hardware frame transfer
			if (_codecContext->hw_device_ctx != null) {
				_hwFrame->hw_frames_ctx = _codecContext->hw_device_ctx;
				ffmpeg.av_hwframe_transfer_data(_hwFrame, _frame, 0)
					.ThrowFFmpegException();
				frame           = *_hwFrame;
				frame.pts       = _frame->pts;
				frame.duration  = _frame->duration;
				frame.time_base = _frame->time_base;
			} else {
				frame = *_frame;
			}

			return true;
		}

		/// <summary>
		/// Decodes a packet into a frame (convenience method).
		/// For audio, use SendPacket + ReceiveFrame loop to get all frames.
		/// Returns true if at least one frame was successfully decoded.
		/// </summary>
		public bool Decode(AVPacket packet, out AVFrame frame) {
			frame = default;

			if (!SendPacket(packet))
				return false;

			return ReceiveFrame(out frame);
		}

		/// <summary>
		/// Flushes the decoder buffers after seeking.
		/// </summary>
		public void Flush() {
			if (!IsValid) return;
			ffmpeg.avcodec_flush_buffers(_codecContext);
		}

		/// <summary>
		/// Converts a frame's PTS to seconds.
		/// </summary>
		public double GetFrameTime(AVFrame frame)
			=> frame.pts * TimeBase;

		/// <summary>
		/// Converts a frame's duration to seconds.
		/// </summary>
		public double GetFrameDuration(AVFrame frame)
			=> frame.duration * TimeBase;

		public void Dispose() {
			var frame = _frame;
			if (frame != null) ffmpeg.av_frame_free(&frame);

			var hwFrame = _hwFrame;
			if (hwFrame != null) ffmpeg.av_frame_free(&hwFrame);

			if (_codecContext != null)
				fixed (AVCodecContext** codecPtr = &_codecContext)
					ffmpeg.avcodec_free_context(codecPtr);
		}
	}
}