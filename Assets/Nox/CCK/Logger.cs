using System;
using UnityEngine;
using ULogger = UnityEngine.Debug;
using Object = UnityEngine.Object;
using System.IO;

namespace Nox.CCK
{
    public class Logger
    {
        public const long MaxLogSize = 1024 * 1024 * 10; // 10 MB
        public static string LogDir => Path.Combine(Constants.GameAppDataPath, "logs");
        public static string LogFile => Path.Combine(LogDir, "latest.txt");

        public static bool IsInitialized { get; private set; } = false;

        public static void Init()
        {
            if (!Directory.Exists(LogDir))
                Directory.CreateDirectory(LogDir);

            if (File.Exists(LogFile))
            {
                var creationTime = File.GetCreationTime(LogFile);
                var i = 0;
                string newFileName;
                do { newFileName = Path.Combine(LogDir, $"log_{creationTime:yyyy-MM-dd_HH-mm-ss}_{i++}.txt"); }
                while (File.Exists(newFileName));
                File.Move(LogFile, newFileName);
            }
            File.Create(LogFile).Close();
        }

        public static void Log(object message)
        {
            OnLog(LogType.Log, message);
            ULogger.Log(message);
        }

        public static void LogWarning(object message)
        {
            OnLog(LogType.Warning, message);
            ULogger.LogWarning(message);
        }

        public static void LogError(object message)
        {
            OnLog(LogType.Error, message);
            ULogger.LogError(message);
        }

        public static void LogException(Exception exception)
        {
            OnLog(LogType.Exception, exception);
            ULogger.LogException(exception);
        }

        public static void Log(object message, Object context)
        {
            OnLog(LogType.Log, message);
            ULogger.Log(message, context);
        }

        public static void LogWarning(object message, Object context)
        {
            OnLog(LogType.Warning, message);
            ULogger.LogWarning(message, context);
        }

        public static void LogError(object message, Object context)
        {
            OnLog(LogType.Error, message);
            ULogger.LogError(message, context);
        }

        public static void LogException(Exception exception, Object context)
        {
            OnLog(LogType.Exception, exception);
            ULogger.LogException(exception, context);
        }

        public static void OnLog(LogType type, object message)
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

            string methodName = frames[old].GetMethod().Name;
            string className = frames[old].GetMethod().DeclaringType.Name;

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
                className = frames[old].GetMethod().DeclaringType.Name;
            }

            File.AppendAllText(LogFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{type}] [{className}.{methodName}] {message}{Environment.NewLine}");
        }
    }
}