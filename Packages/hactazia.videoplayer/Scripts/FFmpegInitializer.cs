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

		public static unsafe string FFmpegDescribe(this int error) {
			const int bufferSize = 1024;
			var       buffer     = stackalloc byte[bufferSize];
			ffmpeg.av_strerror(error, buffer, bufferSize);
			return System.Runtime.InteropServices.Marshal.PtrToStringAnsi((IntPtr)buffer) ?? $"error {error}";
		}

		private static av_log_set_callback_callback _logCallback;

	private static unsafe void LogCallback(void* ptr, int level, string fmt, byte* vl) {
		if (level > ffmpeg.av_log_get_level()) return;
		const int lineSize = 1024;
		var lineBuffer = stackalloc byte[lineSize];
		var printPrefix = 1;
		ffmpeg.av_log_format_line(ptr, level, fmt, vl, lineBuffer, lineSize, &printPrefix);
		var line = System.Runtime.InteropServices.Marshal.PtrToStringAnsi((IntPtr)lineBuffer);
		
		// Filter out verbose HLS/HTTPS logs
		if (!string.IsNullOrEmpty(line)) {
			if (line.Contains("HLS request for url") || 
			    line.Contains("Opening '") || 
			    line.Contains("Skip ('"))
				return;
		}
		
		Debug.Log($"[FFmpeg] {line}");
	}		public static void EnsureInitialized(string explicitRootPath = null) {
			lock (SyncRoot) {
				if (_initialized) {
					return;
				}

				_rootPath = string.IsNullOrEmpty(explicitRootPath)
					? DeriveDefaultRootPath()
					: explicitRootPath;

				if (!Directory.Exists(_rootPath)) {
					Debug.LogWarning($"FFmpeg root path '{_rootPath}' does not exist. Video playback will likely fail.");
				} else {
					Debug.Log($"[VideoPlayer] FFmpeg root path set to: {_rootPath}");
				}

				ffmpeg.RootPath = _rootPath;
				
				unsafe {
					_logCallback = LogCallback;
					ffmpeg.av_log_set_callback(_logCallback);
				}
				ffmpeg.av_log_set_level(ffmpeg.AV_LOG_VERBOSE);
				
				Debug.Log("[VideoPlayer] Initializing FFmpeg network...");
				ffmpeg.avformat_network_init().ThrowFFmpegException("avformat_network_init");
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
			return Path.GetFullPath(Path.Combine(dataPath, "..", "Packages", "hactazia.videoplayer", "Plugins"));
		}

		// No explicit log callback is registered to avoid marshaling headaches; FFmpeg will log to stderr instead.
	}
}