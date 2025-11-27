using System;
using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using UnityEngine;
using UnityEngine.Events;

namespace Hactazia.FFPlay {
	/// <summary>
	/// Données d'événement FFmpeg incluant le contexte et le message
	/// </summary>
	[Serializable]
	public unsafe class FFmpegLogEventArgs {
		public void*  Context { get; }
		public int    Level   { get; }
		public string Message { get; }

		public FFmpegLogEventArgs(void* ptr, int level, string line) {
			Context = ptr;
			Level   = level;
			Message = line;
		}
	}

	/// <summary>
	/// UnityEvent pour les logs FFmpeg
	/// </summary>
	[Serializable]
	public class FFmpegLogEvent : UnityEvent<FFmpegLogEventArgs> { }

	/// <summary>
	/// Handles FFmpeg bootstrap so native binaries are resolved once per domain.
	/// </summary>
	public static class Initializer {
		private static readonly object SyncRoot = new();
		private static          bool   _initialized;
		private static          string _rootPath;

		/// <summary>
		/// Event déclenché pour chaque log FFmpeg avec le contexte et le message
		/// </summary>
		public static readonly FFmpegLogEvent OnFFmpegLog = new();

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

			// Extraire le contexte (nom de la classe/fonction FFmpeg)
			string context = "FFmpeg";
			if (ptr != null) {
				var avcl = (AVClass**)ptr;
				if (*avcl != null) {
					var className = Marshal.PtrToStringAnsi((IntPtr)(*avcl)->class_name);
					if (!string.IsNullOrEmpty(className)) {
						context = className;
					}
				}
			}

			Debug.Log($"[FFmpeg] {line}");

			// Invoquer l'événement avec les données complètes
			OnFFmpegLog?.Invoke(new FFmpegLogEventArgs(ptr, level, line ?? string.Empty));
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