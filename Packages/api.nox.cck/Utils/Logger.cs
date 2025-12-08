using ULogger = UnityEngine.Debug;
using Object = UnityEngine.Object;
using System.IO;
using System;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using ILogger = UnityEngine.ILogger;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Nox.CCK.Utils {
	public class Logger : ILogger {
		public const long MaxLogSize = 1024 * 1024 * 10; // 10 MB

		public static string LogDir
			=> Path.Combine(Constants.AppPath, "logs");

		public static string LogFile
			=> Path.Combine(LogDir, "latest.log");

		public static byte LogID         { get; private set; } = 0;
		public static bool IsInitialized { get; private set; } = false;

		private static readonly object FileLock = new();

		// ILogger implementation
		public ILogHandler         logHandler    { get; set; } = ULogger.unityLogger.logHandler;
		public bool                logEnabled    { get; set; } = true;
		public UnityEngine.LogType filterLogType { get; set; } = UnityEngine.LogType.Log;

		private Logger() {
			// Constructor privé pour le singleton
		}

		#if UNITY_EDITOR
		[InitializeOnLoadMethod]
		private static void EditorInit() {
			Init();
			IsInitialized = true;
		}

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
		public static void ShowProgress(string title, string message, float progress) {
			EditorUtility.DisplayProgressBar(title, message, progress);
			OnProgress.Invoke(true, title, message, progress);
		}

		/// <summary>
		/// Clears the progress bar in the Editor.
		/// </summary>
		public static void ClearProgress() {
			EditorUtility.ClearProgressBar();
			OnProgress.Invoke(false, null, null, -1f);
		}

		#else
		public static void ShowProgress(string title, string message, float progress)
			=> OnProgress.Invoke(true, title, message, progress);
		
		public static void ClearProgress()
			=> OnProgress.Invoke(false, null, null, -1f);

		#endif

		public static readonly UnityEvent<bool, string, string, float>     OnProgress = new();
		public static readonly UnityEvent<LogType, string, string, Object> OnLog      = new();

		public static void Init() {
			lock (FileLock) {
				if (!Directory.Exists(LogDir))
					Directory.CreateDirectory(LogDir);

				if (File.Exists(LogFile)) {
					var    creationTime = File.GetCreationTime(LogFile);
					var    i            = 0;
					string newFileName;
					do {
						newFileName = Path.Combine(LogDir, $"log_{creationTime:yyyy-MM-dd_HH-mm-ss}_{i++}.log");
					} while (File.Exists(newFileName));

					File.Move(LogFile, newFileName);
				}

				// Create the file
				File.Create(LogFile).Dispose();
				LogID = (byte)new System.Random().Next(byte.MinValue, byte.MaxValue);

				File.AppendAllLines(
					LogFile, new[] {
						$"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {LogID:X2}] Logger initialized."
					}
				);
			}
		}

		public static void Log(object message, string tag = null)
			=> Print(LogType.Log, message, tag: tag);

		public static void LogWarning(object message, string tag = null)
			=> Print(LogType.Warning, message, tag: tag);

		public static void LogError(object message, string tag = null)
			=> Print(LogType.Error, message, tag: tag);

		public static void LogDebug(object message, string tag = null)
			=> Print(LogType.Debug, message, tag: tag);

		public static void LogException(Exception exception, string tag = null)
			=> Print(LogType.Exception, exception, tag: tag);

		public static void Log(object message, Object context, string tag = null)
			=> Print(LogType.Log, message, context, tag: tag);

		public static void LogWarning(object message, Object context, string tag = null)
			=> Print(LogType.Warning, message, context, tag: tag);

		public static void LogError(object message, Object context, string tag = null)
			=> Print(LogType.Error, message, context, tag: tag);

		public static void LogException(Exception exception, Object context, string tag = null)
			=> Print(LogType.Exception, exception, context, tag: tag);

		public static void LogDebug(object message, Object context, string tag = null)
			=> Print(LogType.Debug, message, context, tag: tag);

		// ILogger interface methods
		public void LogFormat(UnityEngine.LogType logType, string format, params object[] args) {
			if (!logEnabled) return;
			var message = string.Format(format, args);
			Print(ConvertLogType(logType), message);
		}

		public void LogFormat(UnityEngine.LogType logType, Object context, string format, params object[] args) {
			if (!logEnabled) return;
			var message = string.Format(format, args);
			Print(ConvertLogType(logType), message, context);
		}

		public void LogException(Exception exception, Object context) {
			if (!logEnabled) return;
			Print(LogType.Exception, exception, context);
		}

		public bool IsLogTypeAllowed(UnityEngine.LogType logType) {
			if (!logEnabled) return false;
			return (int)filterLogType >= (int)logType;
		}

		public void Log(UnityEngine.LogType logType, object message) {
			if (!logEnabled) return;
			Print(ConvertLogType(logType), message);
		}

		public void Log(UnityEngine.LogType logType, object message, Object context) {
			if (!logEnabled) return;
			Print(ConvertLogType(logType), message, context);
		}

		public void Log(UnityEngine.LogType logType, string tag, object message) {
			if (!logEnabled) return;
			Print(ConvertLogType(logType), message, tag: tag);
		}

		public void Log(UnityEngine.LogType logType, string tag, object message, Object context) {
			if (!logEnabled) return;
			Print(ConvertLogType(logType), message, context, tag: tag);
		}

		public void Log(object message) {
			if (!logEnabled) return;
			Print(LogType.Log, message);
		}

		public void Log(string tag, object message) {
			if (!logEnabled) return;
			Print(LogType.Log, message, tag: tag);
		}

		public void Log(string tag, object message, Object context) {
			if (!logEnabled) return;
			Print(LogType.Log, message, context, tag: tag);
		}

		public void LogWarning(string tag, object message) {
			if (!logEnabled) return;
			Print(LogType.Warning, message, tag: tag);
		}

		public void LogWarning(string tag, object message, Object context) {
			if (!logEnabled) return;
			Print(LogType.Warning, message, context, tag: tag);
		}

		public void LogError(string tag, object message) {
			if (!logEnabled) return;
			Print(LogType.Error, message, tag: tag);
		}

		public void LogError(string tag, object message, Object context) {
			if (!logEnabled) return;
			Print(LogType.Error, message, context, tag: tag);
		}

		public void LogException(Exception exception) {
			if (!logEnabled) return;
			Print(LogType.Exception, exception);
		}

		private static LogType ConvertLogType(UnityEngine.LogType type)
			=> type switch {
				UnityEngine.LogType.Error     => LogType.Error,
				UnityEngine.LogType.Assert    => LogType.Assert,
				UnityEngine.LogType.Warning   => LogType.Warning,
				UnityEngine.LogType.Log       => LogType.Log,
				UnityEngine.LogType.Exception => LogType.Exception,
				_                             => LogType.Log
			};


		public static readonly string[] IgnoreStack = {
			"AsyncUniTask",
			"AwaiterActions",
			"CompletionSource",
			"InvokableCall.Invoke",
			"AsyncUniTask`1.Run",
			"AsyncUniTask`2.Run",
			"AsyncUniTaskMethodBuilder.Start",
			"UnityEvent.Invoke",
			"UnityWebRequestAsyncOperationConfiguredSource.MoveNext",
			"UnityWebRequestAsyncOperationConfiguredSource.Continuation",
			"AsyncOperation.InvokeCompletionEvent",
			"AsyncOperation.InvokeCompletionEvent",
			"PlayerLoopRunner.RunCore",
			"ContinuationQueue.RunCore",
			"ContinuationQueue.Update",
			"ContinuationQueue.Run",
			"PlayerLoopHelper.ForceEditorPlayerLoopUpdate"
		};

		// ReSharper disable Unity.PerformanceAnalysis
		public static void Print(LogType type, object message, Object context = null, string tag = null) {
			
			message ??= "<null>";
			OnLog.Invoke(type, tag, message.ToString(), context);
			
			if (type == LogType.Debug && !Config.Load().Get("debug.logging", Application.isEditor))
				return;
			
			try {
				// Capture stack trace synchronously on the calling thread
				var stackTrace = new System.Diagnostics.StackTrace(2, true);
				var frames     = stackTrace.GetFrames();
				var timestamp  = DateTime.Now;


				// Write to file in a separate thread
				ThreadPool.QueueUserWorkItem(
					_ => {
						try {
							var old        = 0;
							var methodName = "<UnknownMethod>";
							var className  = "<UnknownClass>";

							if (frames is { Length: > 0 }) {
								methodName = frames[old].GetMethod().Name;
								className  = frames[old].GetMethod().DeclaringType?.Name ?? className;
								while ((className.StartsWith("<") || IgnoreStack.Any(s => $"{className}.{methodName}".Contains(s))) && frames.Length > ++old) {
									methodName = frames[old].GetMethod().Name;
									className  = frames[old].GetMethod().DeclaringType?.Name ?? className;
								}
							}

							var logMessage       = message.ToString();
							var stackTraceString = stackTrace.ToString();

							lock (FileLock) {
								if (!IsInitialized) {
									Init();
									IsInitialized = true;
								} else if (!File.Exists(LogFile) || new FileInfo(LogFile).Length > MaxLogSize) {
									Init();
									// Note: Avoid recursive call here, just log directly
									using (var fs = new FileStream(LogFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
									using (var writer = new StreamWriter(fs)) {
										writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {LogID:X2}] [Log] [Logger.OnLog] Log file exceeded maximum size and was rotated.");
									}
								}

								using (var fs = new FileStream(LogFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
								using (var writer = new StreamWriter(fs)) {
									writer.WriteLine($"[{timestamp:yyyy-MM-dd HH:mm:ss.fff} {LogID:X2}] [{type}] [{(string.IsNullOrEmpty(tag) ? "" : tag + ":")}{className}.{methodName}] {logMessage}");

									if (type is LogType.Error or LogType.Exception)
										writer.Write(stackTraceString + "\n");
								}
							}
						} catch (Exception e) {
							// Can't use custom logging here to avoid infinite recursion
							ULogger.LogException(e);
						}
					}
				);

				switch (type) {
					case LogType.Log:       ULogger.Log($"[<color=cyan>{type}</color>] {(string.IsNullOrEmpty(tag) ? "" : $"[{tag}]")} {message}", context); break;
					case LogType.Warning:   ULogger.LogWarning($"[<color=yellow>{type}</color>] {message}", context); break;
					case LogType.Error:     ULogger.LogError($"[<color=red>{type}</color>] {message}", context); break;
					case LogType.Exception: ULogger.LogException(message as Exception, context); break;
					case LogType.Debug:     ULogger.Log($"[<color=green>{type}</color>] {message}", context); break;
					case LogType.Assert:    ULogger.LogAssertion($"[<color=magenta>{type}</color>] {message}", context); break;
					#if UNITY_EDITOR
					case LogType.Editor: ULogger.Log($"[<color=blue>{type}</color>] {message}", context); break;
					#endif
					default: ULogger.Log($"[<color=white>{type}</color>] {message}", context); break;
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