using UnityEngine;

namespace Nox
{
    public class Debug
    {
        private const string Prefix = "[Nox] ";
        public static void Log(object message, Object context = null) => UnityEngine.Debug.Log(Prefix + message.ToString(), context);
        public static void LogWarning(object message, Object context = null) => UnityEngine.Debug.LogWarning(Prefix + message.ToString(), context);
        public static void LogError(object message, Object context = null) => UnityEngine.Debug.LogError(Prefix + message.ToString(), context);
        public static void LogFormat(string format, params object[] args) => UnityEngine.Debug.LogFormat(Prefix + format, args);
        public static void LogWarningFormat(string format, params object[] args) => UnityEngine.Debug.LogWarningFormat(Prefix + format, args);
        public static void LogErrorFormat(string format, params object[] args) => UnityEngine.Debug.LogErrorFormat(Prefix + format, args);
    }
}