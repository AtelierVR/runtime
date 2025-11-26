using Cysharp.Threading.Tasks;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using LogType = Nox.CCK.Utils.LogType;

namespace api.nox.control.handlers {
	public class LoggerHandler {
		public static void Listen() {
			Logger.OnProgress.AddListener(OnProgress);
			Logger.OnLog.AddListener(OnLog);
		}

		private static void OnLog(LogType type, string message, string tag, Object context) {
			var clients = Main.Server.GetClients();
			foreach (var client in clients)
				client.Send("logger:log", type.ToString(), message, tag).Forget();
		}

		private static void OnProgress(bool active, string title, string message, float progress) {
			var clients = Main.Server.GetClients();
			foreach (var client in clients)
				client.Send("logger:progress", active, title, message, progress).Forget();
		}

		public static void Dispose() {
			Logger.OnProgress.RemoveListener(OnProgress);
			Logger.OnLog.RemoveListener(OnLog);
		}
	}
}