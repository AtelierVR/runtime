using System;
using System.Runtime.InteropServices;
using System.Threading;
using FFmpeg.AutoGen;
using Hactazia.VideoPlayer.Core;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Hactazia.VideoPlayer.Components {
	[DisallowMultipleComponent]
	public sealed class VideoOutput : MonoBehaviour {
		public event Action<Texture2D> OnFrameReady;

		[Tooltip("Force output width. Set to 0 to use the source width.")]
		public int overrideWidth;

		[Tooltip("Force output height. Set to 0 to use the source height.")]
		public int overrideHeight;

		[Tooltip("Flip the texture vertically before presenting.")]
		public bool flipTexture = true;

		[Header("Runtime Data")]
		public long pts;

		private          Texture2D         _texture;
		private          byte[]            _frameData  = Array.Empty<byte>();
		private          byte[]            _backBuffer = Array.Empty<byte>();
		private readonly Mutex             _mutex      = new();
		private          NativeArray<byte> _rawData;
		private          int               _frameWidth;
		private          int               _frameHeight;
		private          long              _pendingPts;

		public void PresentFrame(AVFrame frame) {
			var       width      = overrideWidth  > 0 ? overrideWidth : frame.width;
			var       height     = overrideHeight > 0 ? overrideHeight : frame.height;
			const int pixelWidth = 3;
			var       length     = width * height * pixelWidth;

			EnsureBuffers(length);

			if (!TryConvertFrame(frame, width, height))
				return;


			if (!_mutex.WaitOne())
				return;

			try {
				_pendingPts  = frame.pts;
				_frameWidth  = width;
				_frameHeight = height;

				if (flipTexture)
					CopyAndFlip(_backBuffer, _frameData, width, height, pixelWidth);
				else Array.Copy(_backBuffer, _frameData, length);
			} finally {
				_mutex.ReleaseMutex();
			}
		}

		private void Update() {
			if (_pendingPts == pts)
				return;
			if (!_mutex.WaitOne(0))
				return;

			try {
				pts = _pendingPts;
				DisplayBytes(_frameData, _frameWidth, _frameHeight);
				OnFrameReady?.Invoke(_texture);
			} finally {
				_mutex.ReleaseMutex();
			}
		}

		private void EnsureBuffers(int length) {
			if (_backBuffer.Length != length)
				_backBuffer = new byte[length];
			if (_frameData.Length != length)
				_frameData = new byte[length];
		}

		private unsafe bool TryConvertFrame(AVFrame frame, int width, int height) {
			if (frame.data[0] == null || frame.format == -1)
				return false;

			using var converter = new Converter(
				frame.width, frame.height, (AVPixelFormat)frame.format,
				width, height, AVPixelFormat.AV_PIX_FMT_RGB24
			);

			var converted = converter.Convert(frame);
			var length    = converted.width * converted.height * 3;
			Marshal.Copy((IntPtr)converted.data[0], _backBuffer, 0, length);
			return true;
		}

		private void DisplayBytes(byte[] data, int width, int height) {
			if (data == null || data.Length == 0)
				return;

			if (!_texture)
				_texture = new Texture2D(
					Mathf.Max(1, width),
					Mathf.Max(1, height),
					TextureFormat.RGB24,
					false
				) { name = "VideoOutputTexture" };

			if (_texture.width != width || _texture.height != height) {
				_texture.Reinitialize(width, height);
				_rawData = _texture.GetRawTextureData<byte>();
			} else if (!_rawData.IsCreated || _rawData.Length != data.Length)
				_rawData = _texture.GetRawTextureData<byte>();

			_rawData.CopyFrom(data);
			_texture.Apply(false, false);
		}

		private static unsafe void CopyAndFlip(byte[] src, byte[] dst, int width, int height, int pixelWidth) {
			var rowWidth  = width * pixelWidth;
			var bottomRow = height - 1;

			fixed (byte* srcPtr = src)
			fixed (byte* dstPtr = dst)
				for (var y = 0; y < height; y++) {
					var srcRow = srcPtr + y               * rowWidth;
					var dstRow = dstPtr + (bottomRow - y) * rowWidth;
					UnsafeUtility.MemCpy(dstRow, srcRow, rowWidth);
				}
		}

		private void OnDestroy() {
			_rawData = default;
			if (!_texture) return;
			DestroyImmediate(_texture);
			_texture = null;
		}
	}
}