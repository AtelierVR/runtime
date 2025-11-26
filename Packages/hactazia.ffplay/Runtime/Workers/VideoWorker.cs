using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using FFmpeg.AutoGen;
using FFmpeg.Unity.Helpers;
using Hactazia.FFPlay.Helpers;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Events;

namespace Hactazia.FFPlay {
	public class VideoWorker : BaseWorker {
		public readonly UnityEvent<Texture2D>  OnDisplay = new();
		public readonly UnityEvent<Vector2Int> OnResize  = new();

		[Header("Settings")]
		public bool flipTexture = true;

		[Header("Runtime Data")]
		public Texture2D image;

		public Vector2Int dims;

		private const int BytesPerPixel = 3;

		private          long   _fPts;
		private          byte[] _fData   = Array.Empty<byte>();
		private          byte[] _bBuffer = Array.Empty<byte>();
		private readonly Mutex  _mutex   = new();

		[Tooltip("Flags whether the texture data should be flipped on the Y axis or not. Minor performance cost when enabled.")]
		private NativeArray<byte> _tData;

		public void PlayPacket(AVFrame frame) {
			var dim = new Vector2Int(
				frame.width,
				frame.height
			);

			var len = dim.x * dim.y * BytesPerPixel;

			if (_bBuffer.Length != len)
				_bBuffer = new byte[len];

			if (!SaveFrame(frame, _bBuffer, dim.x, dim.y))
				return;

			if (!_mutex.WaitOne())
				return;

			try {
				_fPts = frame.pts;
				dims  = dim;

				if (_fData.Length != len)
					_fData = new byte[len];

				if (flipTexture)
					CopyAndFlip(_bBuffer, _fData, dim.x, dim.y);
				else Array.Copy(_bBuffer, _fData, len);
			} finally {
				_mutex.ReleaseMutex();
			}
		}

		private void Update() {
			if (_fPts == pts || !_mutex.WaitOne(0))
				return;

			try {
				pts = _fPts;
				DisplayBytes(_fData, dims.x, dims.y);
				OnDisplay.Invoke(image);
			} finally {
				_mutex.ReleaseMutex();
			}
		}

		private void DisplayBytes(byte[] data, int width, int height) {
			if (data == null || data.Length == 0)
				return;

			if (!image)
				image = new Texture2D(16, 16, TextureFormat.RGB24, false);

			if (image.width != width || image.height != height) {
				image.Reinitialize(width, height);
				_tData = image.GetRawTextureData<byte>();
				OnResize.Invoke(new Vector2Int(width, height));
			}

			_tData.CopyFrom(data);
			image.Apply(false);
		}

		#region Utils

		[ThreadStatic]
		private static byte[] _line;

		private static unsafe bool SaveFrame(AVFrame frame, byte[] texture, int width, int height) {
			if (frame.data[0] == null || frame.format == -1 || texture == null)
				return false;

			using var converter = new Converter(
				new Size(frame.width, frame.height),
				(AVPixelFormat)frame.format,
				new Size(width, height),
				AVPixelFormat.AV_PIX_FMT_RGB24
			);

			var convFrame = converter.Convert(frame);
			var len       = convFrame.width * convFrame.height * BytesPerPixel;

			if (texture.Length < len) {
				Debug.LogError($"Texture buffer too small: need {len}, have {texture.Length}");
				return false;
			}

			if (_line == null || _line.Length < len)
				_line = new byte[len];

			Marshal.Copy((IntPtr)convFrame.data[0], _line, 0, len);
			Array.Copy(_line, 0, texture, 0, len);
			return true;
		}

		private static unsafe void CopyAndFlip(byte[] src, byte[] dst, int width, int height) {
			var rowWidth      = width * 3;
			var heightLessOne = height - 1;
			fixed (byte* srcPtr = src)
			fixed (byte* dstPtr = dst)
				for (var y = 0; y < height; y++)
					UnsafeUtility.MemCpy(
						dstPtr + (heightLessOne - y) * rowWidth,
						srcPtr + y                   * rowWidth,
						rowWidth
					);
		}

		#endregion

		private void OnDestroy() {
			if (_tData.IsCreated)
				_tData.Dispose();

			_mutex?.Dispose();

			if (image)
				Destroy(image);
		}
	}
}