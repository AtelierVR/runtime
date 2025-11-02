using System;
using Object = UnityEngine.Object;

namespace Nox.CCK.Mods.Loggers {
	/// <summary>
	/// API for logging messages and exceptions.
	/// </summary>
	public interface ILoggerAPI {
		/// <summary>
		/// Logs an informational message.
		/// </summary>
		/// <param name="message">The message to log.</param>
		public void Log(string message);

		/// <summary>
		/// Logs a warning message.
		/// </summary>
		/// <param name="message">The warning message to log.</param>
		public void LogWarning(string message);

		/// <summary>
		/// Logs an error message.
		/// </summary>
		/// <param name="message">The error message to log.</param>
		public void LogError(string message);

		/// <summary>
		/// Logs a debug message.
		/// </summary>
		/// <param name="message">The debug message to log.</param>
		public void LogDebug(string message);

		/// <summary>
		/// Logs an exception.
		/// </summary>
		/// <param name="exception">The exception to log.</param>
		public void LogException(Exception exception);

		/// <summary>
		/// Logs an informational message with a Unity context object.
		/// </summary>
		/// <param name="message">The message to log.</param>
		/// <param name="context">The Unity object context.</param>
		public void Log(string message, Object context);

		/// <summary>
		/// Logs a warning message with a Unity context object.
		/// </summary>
		/// <param name="message">The warning message to log.</param>
		/// <param name="context">The Unity object context.</param>
		public void LogWarning(string message, Object context);

		/// <summary>
		/// Logs an error message with a Unity context object.
		/// </summary>
		/// <param name="message">The error message to log.</param>
		/// <param name="context">The Unity object context.</param>
		public void LogError(string message, Object context);

		/// <summary>
		/// Logs a debug message with a Unity context object.
		/// </summary>
		/// <param name="message">The debug message to log.</param>
		/// <param name="context">The Unity object context.</param>
		public void LogDebug(string message, Object context);

		/// <summary>
		/// Logs an exception with a Unity context object.
		/// </summary>
		/// <param name="exception">The exception to log.</param>
		/// <param name="context">The Unity object context.</param>
		public void LogException(Exception exception, Object context);
	}
}