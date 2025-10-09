using System;
using System.Collections.Generic;
using FFmpeg.AutoGen;
using UnityEngine;
using byte_ptr4 = FFmpeg.AutoGen.byte_ptrArray4;
using int4 = FFmpeg.AutoGen.int_array4;
using byte_ptr8 = FFmpeg.AutoGen.byte_ptrArray8;
using int8 = FFmpeg.AutoGen.int_array8;

namespace Hactazia.VideoPlayer.Core {
	internal sealed unsafe class Converter : IDisposable {
		private readonly int           _destinationWidth;
		private readonly int           _destinationHeight;
		private readonly AVPixelFormat _destinationFormat;
		private readonly SwsContext*   _conversionContext;
		private          bool          _disposed;

		public Converter(int sourceWidth,      int sourceHeight,      AVPixelFormat sourceFormat,
			int              destinationWidth, int destinationHeight, AVPixelFormat destinationFormat) {
			_destinationWidth  = destinationWidth;
			_destinationHeight = destinationHeight;
			_destinationFormat = destinationFormat;

			_conversionContext = ffmpeg.sws_getContext(
				sourceWidth,
				sourceHeight,
				sourceFormat,
				destinationWidth,
				destinationHeight,
				destinationFormat,
				ffmpeg.SWS_FAST_BILINEAR,
				null,
				null,
				null
			);

			if (_conversionContext == null) 
				throw new InvalidOperationException("Unable to create FFmpeg sws conversion context.");
		}

		public AVFrame Convert(AVFrame sourceFrame, int alignment = -1) {
			EnsureNotDisposed();

			var align = alignment > 0 ? alignment : DetermineAlignment(sourceFrame);

			byte_ptr4 dstData  = default;
			int4      destines = default;

			ffmpeg.av_image_alloc(ref dstData, ref destines, _destinationWidth, _destinationHeight, _destinationFormat, align)
				.ThrowFFmpegException("av_image_alloc");

			var basePtr = (IntPtr)dstData[0];

			destines[0] = _destinationWidth * 3;
			destines[1] = 0;
			destines[2] = 0;
			destines[3] = 0;

			var result = ffmpeg.sws_scale(
				_conversionContext,
				sourceFrame.data,
				sourceFrame.linesize,
				0,
				sourceFrame.height,
				dstData,
				destines
			);

			if (result < 0) {
				FreeImage(basePtr);
				result.ThrowFFmpegException("sws_scale");
			}

			var managedData = new byte_ptr8();
			managedData.UpdateFrom(dstData);
			var lines = new int8();
			lines.UpdateFrom(destines);

			if (basePtr != IntPtr.Zero)
				_freeList.Add(basePtr);

			return new AVFrame {
				data     = managedData,
				linesize = lines,
				width    = _destinationWidth,
				height   = _destinationHeight,
				format   = (int)_destinationFormat
			};
		}

		public void Dispose() {
			if (_disposed)
				return;

			_disposed = true;

			foreach (var pointer in _freeList)
				FreeImage(pointer);

			_freeList.Clear();

			if (_conversionContext != null)
				ffmpeg.sws_freeContext(_conversionContext);
		}

		private readonly List<IntPtr> _freeList = new();

		private static void FreeImage(IntPtr pointer) {
			if (pointer != IntPtr.Zero)
				ffmpeg.av_free((void*)pointer);
		}

		private static int DetermineAlignment(AVFrame frame) {
			var alignment = 1;
			for (uint i = 1; i <= 64; i *= 2)
				if (Mathf.Abs(frame.linesize[0]) % i == 0 && Mathf.Abs(frame.linesize[1]) % i == 0 && Mathf.Abs(frame.linesize[2]) % i == 0) {
					alignment = (int)i;
				} else break;

			return Mathf.Max(1, alignment);
		}

		private void EnsureNotDisposed() {
			if (_disposed) throw new ObjectDisposedException(nameof(Converter));
		}
	}
}