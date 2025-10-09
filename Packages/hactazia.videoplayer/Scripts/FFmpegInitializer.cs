using System;
using System.IO;
using FFmpeg.AutoGen;
using UnityEngine;

namespace Hactazia.VideoPlayer {
	/// <summary>
	/// Handles FFmpeg bootstrap so native binaries are resolved once per domain.
	/// </summary>
	internal static class FFmpegInitializer {
		private static readonly object SyncRoot = new();
		private static          bool   _initialized;
		private static          string _rootPath;

		public static int ThrowFFmpegException(this int error, string api = "ffmpeg") {
			if (error >= 0)
				return error;
			throw new InvalidOperationException($"{api} failed: {FFmpegDescribe(error)}");
		}

		private static unsafe string FFmpegDescribe(this int error) {
			const int bufferSize = 1024;
			var       buffer     = stackalloc byte[bufferSize];
			ffmpeg.av_strerror(error, buffer, bufferSize);
			return System.Runtime.InteropServices.Marshal.PtrToStringAnsi((IntPtr)buffer) ?? $"error {error}";
		}

		public static void EnsureInitialized(string explicitRootPath = null) {
			lock (SyncRoot) {
				if (_initialized) {
					return;
				}

				_rootPath = string.IsNullOrEmpty(explicitRootPath)
					? DeriveDefaultRootPath()
					: explicitRootPath;

				if (!Directory.Exists(_rootPath)) {
					Debug.LogWarning($"FFmpeg root path '{_rootPath}' does not exist. Video playback will likely fail.");
				}

				ffmpeg.RootPath = _rootPath;
				ffmpeg.av_log_set_level(ffmpeg.AV_LOG_WARNING);
				ffmpeg.avformat_network_init().ThrowFFmpegException("avformat_network_init");
				ffmpeg.avdevice_register_all();
				_initialized = true;
			}
		}

		public static void Shutdown() {
			lock (SyncRoot) {
				if (!_initialized) return;
				ffmpeg.avformat_network_deinit();
				_initialized = false;
			}
		}

		private static string DeriveDefaultRootPath() {
			#if UNITY_EDITOR
			var dataPath = Application.dataPath;
			#else
			var dataPath = Application.streamingAssetsPath;
			#endif
			return Path.GetFullPath(Path.Combine(dataPath, "..", "Packages", "hactazia.videoplayer", "Plugins"));
		}

		// No explicit log callback is registered to avoid marshaling headaches; FFmpeg will log to stderr instead.
	}
}