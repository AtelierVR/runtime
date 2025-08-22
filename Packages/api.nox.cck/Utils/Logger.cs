using ULogger = UnityEngine.Debug;
using Object = UnityEngine.Object;
using System.IO;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Nox.CCK.Utils {
	public class Logger {
		public const long MaxLogSize = 1024 * 1024 * 10; // 10 MB

		public static string LogDir
			=> Path.Combine(Constants.AppPath, "logs");

		public static string LogFile
			=> Path.Combine(LogDir, "latest.log");

		public static byte LogID         { get; private set; } = 0;
		public static bool IsInitialized { get; private set; } = false;

		private static readonly object fileLock = new();

		#if UNITY_EDITOR
		[MenuItem("Nox/Logger/Open Latest Log")]
		private static void OpenLatestLog() {
			if (File.Exists(LogFile))
				EditorUtility.OpenWithDefaultApp(LogFile);
			else OpenDialog("Nox Logger", "No log file found.", "OK");
		}

		[MenuItem("Nox/Logger/Reveal Latest Log")]
		private static void RevealLatestLog() {
			if (File.Exists(LogFile))
				EditorUtility.RevealInFinder(LogFile);
			else OpenDialog("Nox Logger", "No log file found.", "OK");
		}

		[MenuItem("Nox/Logger/Clear Logs")]
		private static void ClearLog() {
			var files = Directory.GetFiles(LogDir);
			foreach (var file in files)
				File.Delete(file);
			Init();
		}

		[MenuItem("Nox/Logger/Open Unity Log")]
		private static void OpenUnityLog() {
			var logPath = Application.consoleLogPath;
			if (File.Exists(logPath))
				EditorUtility.OpenWithDefaultApp(logPath);
			else OpenDialog("Nox Logger", "No Unity log file found.", "OK");
		}

		/// <summary>
		/// Opens a dialog in the Editor with a title, message, and buttons.
		/// If cancel is null, it will only have an OK button.
		/// </summary>
		/// <param name="title"></param>
		/// <param name="message"></param>
		/// <param name="ok"></param>
		/// <param name="cancel"></param>
		/// <returns></returns>
		public static bool OpenDialog(string title, string message, string ok, string cancel = null)
			=> string.IsNullOrEmpty(cancel)
				? EditorUtility.DisplayDialog(title, message, ok)
				: EditorUtility.DisplayDialog(title, message, ok, cancel);

		/// <summary>
		/// Displays a progress bar in the Editor.
		/// </summary>
		/// <param name="title"></param>
		/// <param name="message"></param>
		/// <param name="progress"></param>
		public static void ShowProgress(string title, string message, float progress)
			=> EditorUtility.DisplayProgressBar(title, message, progress);

		/// <summary>
		/// Clears the progress bar in the Editor.
		/// </summary>
		public static void ClearProgress()
			=> EditorUtility.ClearProgressBar();

		#endif

		public static void Init() {
			lock (fileLock) {
				if (!Directory.Exists(LogDir))
					Directory.CreateDirectory(LogDir);

				if (File.Exists(LogFile)) {
					var    creationTime = File.GetCreationTime(LogFile);
					int    i            = 0;
					string newFileName;
					do {
						newFileName = Path.Combine(LogDir, $"log_{creationTime:yyyy-MM-dd_HH-mm-ss}_{i++}.log");
					} while (File.Exists(newFileName));

					File.Move(LogFile, newFileName);
				}

				using (var fs = new FileStream(LogFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite)) {
					// Création du fichier sans verrou exclusif
				}

				LogID = (byte)UnityEngine.Random.Range(byte.MinValue, byte.MaxValue);

				File.AppendAllLines(
					LogFile, new[] {
						$"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {LogID:X2}] Logger initialized."
					}
				);
			}
		}

		public static void Log(object message)
			=> OnLog(LogType.Log, message);

		public static void LogWarning(object message)
			=> OnLog(LogType.Warning, message);

		public static void LogError(object message)
			=> OnLog(LogType.Error, message);

		public static void LogDebug(object message)
			=> OnLog(LogType.Debug, message);

		public static void LogException(Exception exception)
			=> OnLog(LogType.Exception, exception);

		public static void Log(object message, Object context)
			=> OnLog(LogType.Log, message, context);

		public static void LogWarning(object message, Object context)
			=> OnLog(LogType.Warning, message, context);

		public static void LogError(object message, Object context)
			=> OnLog(LogType.Error, message, context);

		public static void LogException(Exception exception, Object context)
			=> OnLog(LogType.Exception, exception, context);

		public static void LogDebug(object message, Object context)
			=> OnLog(LogType.Debug, message, context);

		// ReSharper disable Unity.PerformanceAnalysis
		public static void OnLog(LogType type, object message, Object context = null) {
			if (type == LogType.Debug && !Config.Load().Get(new[] { "debug_logging" }, Application.isEditor))
				return;

			message ??= "<null>";

			try {
				lock (fileLock) {
					if (!IsInitialized) {
						Init();
						IsInitialized = true;
					} else if (!File.Exists(LogFile) || new FileInfo(LogFile).Length > MaxLogSize)
						Init();

					var    stackTrace = new System.Diagnostics.StackTrace(2, true);
					var    frames     = stackTrace.GetFrames();
					int    old        = 0;
					string methodName = "<UnknownMethod>";
					string className  = "<UnknownClass>";

					if (frames != null && frames.Length > 0) {
						methodName = frames[old].GetMethod().Name;
						className  = frames[old].GetMethod().DeclaringType?.Name ?? className;
						while ((className.StartsWith("<") || className.Contains("AsyncUniTask") || className.Contains("AwaiterActions") || className.Contains("CompletionSource")) && frames.Length > ++old) {
							methodName = frames[old].GetMethod().Name;
							className  = frames[old].GetMethod().DeclaringType?.Name ?? className;
						}
					}

					File.AppendAllText(
						LogFile,
						$"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {LogID:X2}] [{type}] [{className}.{methodName}] {message}{Environment.NewLine}"
					);

					if (type == LogType.Error || type == LogType.Exception)
						File.AppendAllText(LogFile, stackTrace + "\n");
				}

				// Log côté Unity
				switch (type) {
					case LogType.Log:       ULogger.Log($"[<color=cyan>{type}</color>] {message}", context); break;
					case LogType.Warning:   ULogger.LogWarning($"[<color=yellow>{type}</color>] {message}", context); break;
					case LogType.Error:     ULogger.LogError($"[<color=red>{type}</color>] {message}", context); break;
					case LogType.Exception: ULogger.LogException(message as Exception, context); break;
					case LogType.Debug:     ULogger.Log($"[<color=green>{type}</color>] {message}", context); break;
				}
			} catch (Exception e) {
				ULogger.LogException(e);
			}
		}
	}

	public enum LogType {
		Error,
		Assert,
		Warning,
		Log,
		Exception,
		Debug,
		#if UNITY_EDITOR
		Editor,
		#endif
	}
}