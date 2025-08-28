using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;

namespace api.nox.statistics.Editor {
	public class StatisticsEditorMenu {
		
		[MenuItem("Nox/Statistics/Open Statistics Folder")]
		public static void OpenStatisticsFolder() {
			string saveDirectory = Path.Combine(Application.persistentDataPath, "NoxStatistics");
			
			// Créer le dossier s'il n'existe pas
			if (!Directory.Exists(saveDirectory)) {
				Directory.CreateDirectory(saveDirectory);
				UnityEngine.Debug.Log($"Created statistics directory: {saveDirectory}");
			}
			
			// Ouvrir le dossier dans l'explorateur
			try {
				#if UNITY_EDITOR_WIN
					Process.Start("explorer.exe", saveDirectory.Replace('/', '\\'));
				#elif UNITY_EDITOR_OSX
					Process.Start("open", saveDirectory);
				#elif UNITY_EDITOR_LINUX
					Process.Start("xdg-open", saveDirectory);
				#endif
				
				UnityEngine.Debug.Log($"Opened statistics folder: {saveDirectory}");
			}
			catch (System.Exception ex) {
				EditorUtility.DisplayDialog("Error", 
					$"Could not open statistics folder:\n{saveDirectory}\n\nError: {ex.Message}", 
					"OK");
				UnityEngine.Debug.LogError($"Error opening statistics folder: {ex.Message}");
			}
		}
		
		[MenuItem("Nox/Statistics/Show Current Statistics")]
		public static void ShowCurrentStatistics() {
			string saveDirectory = Path.Combine(Application.persistentDataPath, "NoxStatistics");
			string currentSessionFile = Path.Combine(saveDirectory, "current_session.json");
			string historyFile = Path.Combine(saveDirectory, "time_history.json");
			
			string message = "Nox Statistics\n\n";
			message += $"Save Directory: {saveDirectory}\n\n";
			
			// Afficher les informations de la session courante
			if (File.Exists(currentSessionFile)) {
				try {
					string json = File.ReadAllText(currentSessionFile);
					var stats = Newtonsoft.Json.JsonConvert.DeserializeObject<TimeStatistics>(json);
					if (stats != null) {
						message += "Current Session:\n";
						message += $"Date: {stats.SessionDate:yyyy-MM-dd HH:mm:ss}\n";
						message += $"Total Time: {stats.FormattedTotalTime}\n";
						message += $"Play Time: {stats.FormattedPlayTime}\n";
						message += $"Editor Time: {stats.FormattedEditorTime}\n\n";
					}
				}
				catch (System.Exception ex) {
					message += $"Error reading current session: {ex.Message}\n\n";
				}
			} else {
				message += "No current session found\n\n";
			}
			
			// Afficher les informations de l'historique
			if (File.Exists(historyFile)) {
				try {
					string json = File.ReadAllText(historyFile);
					var history = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.List<TimeStatistics>>(json);
					if (history != null && history.Count > 0) {
						message += $"History: {history.Count} sessions saved\n";
						message += $"Latest session: {history[history.Count - 1].SessionDate:yyyy-MM-dd HH:mm:ss}";
					}
				}
				catch (System.Exception ex) {
					message += $"Error reading history: {ex.Message}";
				}
			} else {
				message += "No history found";
			}
			
			EditorUtility.DisplayDialog("Nox Statistics", message, "OK");
		}
	}
}
