using System;
using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using UnityEngine;

namespace Hactazia.FFPlay {
	/// <summary>
	/// Handles FFmpeg bootstrap so native binaries are resolved once per domain.
	/// </summary>
	public static class Initializer {
		private static readonly object SyncRoot = new();
		private static          bool   _initialized;
		private static          string _rootPath;

		public static int ThrowFFmpegException(this int error, string api = "ffmpeg") {
			if (error >= 0)
				return error;
			throw new ApplicationException($"{api} failed: {error.FFmpegDescribe()}");
		}

		public static unsafe string FFmpegDescribe(this int error) {
			const int bufferSize = 1024;
			var       buffer     = stackalloc byte[bufferSize];
			ffmpeg.av_strerror(error, buffer, bufferSize);
			return Marshal.PtrToStringAnsi((IntPtr)buffer)
				?? $"error {error}";
		}

		private static av_log_set_callback_callback _logCallback;

		private static unsafe void LogCallback(void* ptr, int level, string fmt, byte* vl) {
			if (level > ffmpeg.av_log_get_level()) return;

			const int lineSize    = 1024;
			var       lineBuffer  = stackalloc byte[lineSize];
			var       printPrefix = 1;
			ffmpeg.av_log_format_line(ptr, level, fmt, vl, lineBuffer, lineSize, &printPrefix);
			var line = Marshal.PtrToStringAnsi((IntPtr)lineBuffer);

			// Debug.Log($"[FFmpeg] {line}");
		}

		public static void EnsureInitialized(string explicitRootPath = null) {
			lock (SyncRoot) {
				if (_initialized)
					return;

				_rootPath = string.IsNullOrEmpty(explicitRootPath)
					? DeriveDefaultRootPath()
					: explicitRootPath;

				if (!Directory.Exists(_rootPath)) {
					Debug.LogWarning($"FFmpeg root path '{_rootPath}' does not exist. Video playback will likely fail.");
				} else Debug.Log($"[VideoPlayer] FFmpeg root path set to: {_rootPath}");

				ffmpeg.RootPath = _rootPath;

				unsafe {
					_logCallback = LogCallback;
					ffmpeg.av_log_set_callback(_logCallback);
				}

				ffmpeg.av_log_set_level(ffmpeg.AV_LOG_VERBOSE);

				Debug.Log("[VideoPlayer] Initializing FFmpeg network...");

				ffmpeg.avformat_network_init()
					.ThrowFFmpegException("avformat_network_init");
				ffmpeg.avdevice_register_all();

				_initialized = true;
				Debug.Log("[VideoPlayer] FFmpeg initialized successfully.");
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
			return Path.GetFullPath(Path.Combine(dataPath, "..", "Packages", "hactazia.ffplay", "Plugins"));
		}
	}
}