using System;
using System.Drawing;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace Hactazia.FFPlay.Core {
	/// <summary>
	/// Converts video frames between pixel formats with caching for efficiency.
	/// Reuses SwsContext when possible to avoid repeated allocations.
	/// </summary>
	public sealed unsafe class FrameConverter : IDisposable {
		private SwsContext*   _context;
		private Size          _srcSize;
		private Size          _dstSize;
		private AVPixelFormat _srcFormat;
		private AVPixelFormat _dstFormat;

		// Cached destination buffer
		private byte_ptrArray4 _dstData;
		private int_array4     _dstLinesize;
		private bool           _bufferAllocated;

		public Size          DestinationSize   => _dstSize;
		public AVPixelFormat DestinationFormat => _dstFormat;

		public FrameConverter() { }

		public FrameConverter(Size srcSize, AVPixelFormat srcFormat, Size dstSize, AVPixelFormat dstFormat) {
			Configure(srcSize, srcFormat, dstSize, dstFormat);
		}

		/// <summary>
		/// Configures or reconfigures the converter for new dimensions/formats.
		/// Reuses existing context if parameters match.
		/// </summary>
		public void Configure(Size srcSize, AVPixelFormat srcFormat, Size dstSize, AVPixelFormat dstFormat) {
			if (_context != null &&
			    _srcSize == srcSize && _dstSize == dstSize &&
			    _srcFormat == srcFormat && _dstFormat == dstFormat)
				return;

			Release();

			_srcSize   = srcSize;
			_dstSize   = dstSize;
			_srcFormat = srcFormat;
			_dstFormat = dstFormat;

			_context = ffmpeg.sws_getContext(
				srcSize.Width, srcSize.Height, srcFormat,
				dstSize.Width, dstSize.Height, dstFormat,
				ffmpeg.SWS_POINT,
				null, null, null
			);

			if (_context == null)
				throw new InvalidOperationException("Failed to create sws context");

			// Allocate destination buffer
			ffmpeg.av_image_alloc(
				ref _dstData, ref _dstLinesize,
				dstSize.Width, dstSize.Height, dstFormat, 1
			).ThrowFFmpegException();

			_bufferAllocated = true;
		}

		/// <summary>
		/// Converts a frame to the configured destination format.
		/// Returns the converted frame data.
		/// </summary>
		public AVFrame Convert(AVFrame source) {
			if (_context == null)
				throw new InvalidOperationException("Converter not configured");

			// Adjust linesize for RGB output
			if (_dstFormat == AVPixelFormat.AV_PIX_FMT_RGB24) {
				_dstLinesize[0] = _dstSize.Width * 3;
				_dstLinesize[1] = 0;
				_dstLinesize[2] = 0;
				_dstLinesize[3] = 0;
			}

			var result = ffmpeg.sws_scale(
				_context,
				source.data, source.linesize,
				0, source.height,
				_dstData, _dstLinesize
			);

			if (result < 0) {
				result.ThrowFFmpegException();
				throw new InvalidOperationException("Frame conversion failed");
			}

			var data = new byte_ptrArray8();
			data.UpdateFrom(_dstData);
			var linesize = new int_array8();
			linesize.UpdateFrom(_dstLinesize);

			return new AVFrame {
				data     = data,
				linesize = linesize,
				width    = _dstSize.Width,
				height   = _dstSize.Height,
				format   = (int)_dstFormat
			};
		}

		/// <summary>
		/// Converts a frame directly to a byte buffer.
		/// More efficient for final output to textures.
		/// </summary>
		public int ConvertToBuffer(AVFrame source, byte[] buffer) {
			if (_context == null)
				throw new InvalidOperationException("Converter not configured");

			var frame    = Convert(source);
			var dataSize = _dstSize.Width * _dstSize.Height * GetBytesPerPixel(_dstFormat);

			if (buffer.Length < dataSize)
				throw new ArgumentException($"Buffer too small: need {dataSize}, have {buffer.Length}");

			Marshal.Copy((IntPtr)frame.data[0], buffer, 0, dataSize);
			return dataSize;
		}

		private static int GetBytesPerPixel(AVPixelFormat format) {
			return format switch {
				AVPixelFormat.AV_PIX_FMT_RGB24  => 3,
				AVPixelFormat.AV_PIX_FMT_RGBA   => 4,
				AVPixelFormat.AV_PIX_FMT_BGRA   => 4,
				AVPixelFormat.AV_PIX_FMT_BGR24  => 3,
				AVPixelFormat.AV_PIX_FMT_GRAY8  => 1,
				_                               => 3
			};
		}

		private void Release() {
			if (_context != null) {
				ffmpeg.sws_freeContext(_context);
				_context = null;
			}

			if (_bufferAllocated) {
				var ptr = _dstData[0];
				ffmpeg.av_freep(&ptr);
				_bufferAllocated = false;
			}
		}

		public void Dispose() => Release();
	}
}
