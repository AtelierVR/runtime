#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.statistics.Editor {
	public class StatisticsEditorMenu {
		[MenuItem("Nox/Statistics/Open Statistics Folder")]
		public static void OpenStatisticsFolder() {
			var saveDirectory = Main.CoreAPI.ConfigAPI.GetFolder();

			try {
				Logger.Log($"Opened statistics folder: {saveDirectory}");
				EditorUtility.RevealInFinder(saveDirectory);
			} catch (System.Exception ex) {
				Logger.OpenDialog(
					"Error",
					$"Could not open statistics folder:\n{saveDirectory}\n\nError: {ex.Message}",
					"OK"
				);
				Logger.LogError($"Error opening statistics folder: {ex.Message}");
			}
		}

		[MenuItem("Nox/Statistics/Show Current Statistics")]
		public static void ShowCurrentStatistics() {
			var saveDirectory      = Main.CoreAPI.ConfigAPI.GetFolder();
			var currentSessionFile = Path.Combine(saveDirectory, "current_session.json");
			var historyFile        = Path.Combine(saveDirectory, "time_history.json");

			var message = "Nox Statistics\n\n";
			message += $"Save Directory: {saveDirectory}\n\n";
			var total = 0f;
			var play  = 0f;
			var edit  = 0f;

			if (File.Exists(currentSessionFile)) {
				try {
					var json  = File.ReadAllText(currentSessionFile);
					var stats = Newtonsoft.Json.JsonConvert.DeserializeObject<TimeStatistics>(json);
					if (stats != null) {
						message += "Current Session:\n";
						message += $"Date: {stats.SessionDate:yyyy-MM-dd HH:mm:ss}\n";
						message += $"Total Time: {TimeStatistics.FormatTime(stats.totalTime)}\n";
						message += $"Play Time: {TimeStatistics.FormatTime(stats.playTime)}\n";
						message += $"Editor Time: {TimeStatistics.FormatTime(stats.editorTime)}\n\n";
						total   += stats.totalTime;
						play    += stats.playTime;
						edit    += stats.editorTime;
					}
				} catch (System.Exception ex) {
					message += $"Error reading current session: {ex.Message}\n\n";
				}
			} else message += "No current session found\n\n";

			if (File.Exists(historyFile)) {
				try {
					var json    = File.ReadAllText(historyFile);
					var history = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.List<TimeStatistics>>(json);
					if (history is { Count: > 0 }) {
						message += $"History: {history.Count} sessions saved\n";
						message += $"Latest session: {history[^1].SessionDate:yyyy-MM-dd HH:mm:ss}";
						total   += history.Sum(entry => entry.totalTime);
						play    += history.Sum(entry => entry.playTime);
						edit    += history.Sum(entry => entry.editorTime);
						message += $"\nTotal Time in History: {TimeStatistics.FormatTime(total)}";
					}
				} catch (System.Exception ex) {
					message += $"Error reading history: {ex.Message}";
				}
			} else message += "No history found";

			if (Logger.OpenDialog("Nox Statistics", message, "OK", "Copy to Clipboard"))
				return;

			var copy = new StringBuilder();
			copy.AppendLine("Nox Statistics");
			copy.AppendLine($"Total Time: {TimeStatistics.FormatTime(total)}");
			copy.AppendLine($"Play Time: {TimeStatistics.FormatTime(play)}");
			copy.AppendLine($"Editor Time: {TimeStatistics.FormatTime(edit)}");
			GUIUtility.systemCopyBuffer = copy.ToString();
			Logger.Log("Statistics copied to clipboard");
		}
	}
}
#endif