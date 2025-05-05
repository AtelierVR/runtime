using ULogger = UnityEngine.Debug;
using Object = UnityEngine.Object;
using System.IO;
using System;

namespace Nox.CCK.Utils
{
    public class Logger
    {
        public const long MaxLogSize = 1024 * 1024 * 10; // 10 MB
        public static string LogDir => Path.Combine(Constants.GameAppDataPath, "logs");
        public static string LogFile => Path.Combine(LogDir, "latest.log");

        public static byte LogID { get; private set; } = 0;

        public static bool IsInitialized { get; private set; } = false;

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Nox/Logger/Open Latest Log")]
        private static void OpenLatestLog()
        {
            if (File.Exists(LogFile))
                UnityEditor.EditorUtility.OpenWithDefaultApp(LogFile);
            else UnityEditor.EditorUtility.DisplayDialog("Nox Logger", "No log file found.", "OK");
        }

        [UnityEditor.MenuItem("Nox/Logger/Reveal Latest Log")]
        private static void RevealLatestLog()
        {
            if (File.Exists(LogFile))
                UnityEditor.EditorUtility.RevealInFinder(LogFile);
            else UnityEditor.EditorUtility.DisplayDialog("Nox Logger", "No log file found.", "OK");
        }

        [UnityEditor.MenuItem("Nox/Logger/Clear Logs")]
        private static void ClearLog()
        {
            var files = Directory.GetFiles(LogDir);
            foreach (var file in files)
                File.Delete(file);
            Init();
        }
#endif

        public static void Init()
        {
            if (!Directory.Exists(LogDir))
                Directory.CreateDirectory(LogDir);

            if (File.Exists(LogFile))
            {
                var creationTime = File.GetCreationTime(LogFile);
                var i = 0;
                string newFileName;
                do
                {
                    newFileName = Path.Combine(LogDir, $"log_{creationTime:yyyy-MM-dd_HH-mm-ss}_{i++}.log");
                } while (File.Exists(newFileName));

                File.Move(LogFile, newFileName);
            }

            File.Create(LogFile).Close();

            LogID = (byte)UnityEngine.Random.Range(byte.MinValue, byte.MaxValue);

            File.AppendAllLines(LogFile,
                new[] { $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {LogID:X2}] Logger initialized." });
        }

        public static void Log(object message)
        {
            OnLog(LogType.Log, message);
            ULogger.Log($"[<color=green>{LogType.Log}</color>] {message}");
        }

        public static void LogWarning(object message)
        {
            OnLog(LogType.Warning, message);
            ULogger.LogWarning($"[<color=yellow>{LogType.Warning}</color>] {message}");
        }

        public static void LogError(object message)
        {
            OnLog(LogType.Error, message);
            ULogger.LogError($"[<color=red>{LogType.Error}</color>] {message}");
        }

        public static void LogDebug(object message)
        {
            OnLog(LogType.Debug, message);
            ULogger.Log($"[<color=cyan>{LogType.Debug}</color>] {message}");
        }

        public static void LogException(Exception exception)
        {
            OnLog(LogType.Exception, exception);
            ULogger.LogException(exception);
        }

        public static void Log(object message, Object context)
        {
            OnLog(LogType.Log, message);
            ULogger.Log($"[<color=green>{LogType.Log}</color>] {message}", context);
        }

        public static void LogWarning(object message, Object context)
        {
            OnLog(LogType.Warning, message);
            ULogger.LogWarning($"[<color=yellow>{LogType.Warning}</color>] {message}", context);
        }

        public static void LogError(object message, Object context)
        {
            OnLog(LogType.Error, message);
            ULogger.LogError($"[<color=red>{LogType.Error}</color>] {message}", context);
        }

        public static void LogException(Exception exception, Object context)
        {
            OnLog(LogType.Exception, exception);
            ULogger.LogException(exception, context);
        }

        public static void LogDebug(object message, Object context)
        {
            OnLog(LogType.Debug, message);
            ULogger.Log($"[<color=cyan>{LogType.Debug}</color>] {message}", context);
        }

        public static void OnLog(LogType type, object message)
        {
            try
            {
                if (!IsInitialized)
                {
                    Init();
                    IsInitialized = true;
                }
                else if (!File.Exists(LogFile) || new FileInfo(LogFile).Length > MaxLogSize)
                    Init();

                var stackTrace = new System.Diagnostics.StackTrace(2, true);
                var old = 0;
                var frames = stackTrace.GetFrames();

                var methodName = frames[old].GetMethod().Name;
                var className = frames[old].GetMethod().DeclaringType.Name;

                while ((className.StartsWith("<")
                        || className.Contains("AsyncUniTaskMethodBuilder")
                        || className.Contains("AsyncUniTask")
                        || className.Contains("PooledDelegate")
                        || className.Contains("AwaiterActions")
                        || className.Contains("UniTaskCompletionSourceCore")
                        || className.Contains("WaitUntilPromise")
                       ) && frames.Length > ++old)
                {
                    methodName = frames[old].GetMethod().Name;
                    var declaringType = frames[old].GetMethod().DeclaringType;
                    if (declaringType != null)
                        className = declaringType.Name;
                }

                File.AppendAllText(LogFile,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {LogID:X2}] [{type}] [{className}.{methodName}] {message}{Environment.NewLine}");
                if (type is LogType.Error or LogType.Exception)
                    File.AppendAllText(LogFile, stackTrace + "\n");
            }
            catch (Exception e)
            {
                ULogger.LogException(e);
            }
        }
    }

    public enum LogType
    {
        Error,
        Assert,
        Warning,
        Log,
        Exception,
        Debug
    }
}