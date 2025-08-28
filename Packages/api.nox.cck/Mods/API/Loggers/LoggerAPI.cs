using System;
using Object = UnityEngine.Object;

namespace Nox.CCK.Mods.Loggers {
	public interface ILoggerAPI {
		public void Log(string message);

		public void LogWarning(string message);

		public void LogError(string message);

		public void LogDebug(string message);

		public void LogException(Exception exception);

		public void Log(string message, Object context);

		public void LogWarning(string message, Object context);

		public void LogError(string message, Object context);

		public void LogDebug(string message, Object context);

		public void LogException(Exception exception, Object context);
	}
}