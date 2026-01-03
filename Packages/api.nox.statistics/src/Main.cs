using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using Nox.CCK.Utils;
using UnityEngine.Serialization;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.statistics {
	public class Main : IMainModInitializer {
		private       PlayTimeTracker _playTimeTracker;
		public static IMainModCoreAPI  CoreAPI;

		public void OnInitializeMain(IMainModCoreAPI api) {
			CoreAPI          = api;
			_playTimeTracker = new PlayTimeTracker();
			_playTimeTracker.Initialize();
		}

		public void OnUpdateMain()
			=> _playTimeTracker?.Update();

		public void OnDisposeMain()
			=> _playTimeTracker?.Dispose();
	}

	[Serializable]
	public class TimeStatistics {
		[JsonProperty("session_date")]
		public long sessionDate;

		[JsonProperty("total_time")]
		public float totalTime;

		[JsonProperty("play_time")]
		public float playTime;

		[JsonProperty("editor_time")]
		public float editorTime;

		[JsonIgnore]
		public DateTime SessionDate {
			get => DateTimeOffset.FromUnixTimeMilliseconds(sessionDate).DateTime;
			set => sessionDate = ((DateTimeOffset)value).ToUnixTimeMilliseconds();
		}

		public TimeStatistics() {
			SessionDate = DateTime.Now;
			totalTime   = 0f;
			playTime    = 0f;
			editorTime  = 0f;
		}

		public static string FormatTime(float seconds)
			=> TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");
	}

	public class PlayTimeTracker : IDisposable {
		// Variables de temps en secondes
		private float _totalTime;
		private float _playTime;
		private float _editorTime;

		private float _sessionStartTime;
		private float _lastUpdateTime;
		private float _lastSaveTime;
		private bool  _isInitialized;
		private bool  _isDisposed;

		private readonly float  _saveInterval;
		private readonly string _saveDirectory;
		private readonly string _currentSessionFile;
		private readonly string _historyFile;

		public PlayTimeTracker() {
			_saveInterval       = Config.Load().Get("settings.statistics.save_interval", 30f);
			_saveDirectory      = Main.CoreAPI.ConfigAPI.GetFolder();
			_currentSessionFile = Path.Combine(_saveDirectory, "current_session.json");
			_historyFile        = Path.Combine(_saveDirectory, "time_history.json");
		}

		public void Initialize() {
			if (_isInitialized) return;

			LoadCurrentSession();

			_sessionStartTime = Time.realtimeSinceStartup;
			_lastUpdateTime   = _sessionStartTime;
			_lastSaveTime     = _sessionStartTime;
			_isInitialized    = true;

			Logger.Log($"PlayTimeTracker initialized. Save directory: {_saveDirectory}");

			SaveCurrentSession();
		}

		public void Update() {
			if (!_isInitialized || _isDisposed) return;

			var currentTime = Time.realtimeSinceStartup;
			var deltaTime   = currentTime - _lastUpdateTime;

			// Met à jour le temps total
			_totalTime += deltaTime;

			// Détermine si on est en mode éditeur ou en jeu
			if (Application.isEditor)
				_editorTime += deltaTime;

			if (Application.isPlaying)
				_playTime += deltaTime;

			_lastUpdateTime = currentTime;

			// Sauvegarde périodique
			if (!(currentTime - _lastSaveTime >= _saveInterval)) return;

			SaveCurrentSession();
			_lastSaveTime = currentTime;
		}

		private void LoadCurrentSession() {
			try {
				if (!File.Exists(_currentSessionFile)) return;

				var json  = File.ReadAllText(_currentSessionFile);
				var stats = JsonConvert.DeserializeObject<TimeStatistics>(json);

				if (stats == null || !IsFromToday(stats.SessionDate)) return;
				_totalTime  = stats.totalTime;
				_playTime   = stats.playTime;
				_editorTime = stats.editorTime;
				Logger.Log($"Loaded current session: Total={GetFormattedTotalTime()}, Play={GetFormattedPlayTime()}, Editor={GetFormattedEditorTime()}");
			} catch (Exception ex) {
				Logger.LogError($"Error loading current session: {ex.Message}");
			}
		}

		private void SaveCurrentSession() {
			try {
				var stats = new TimeStatistics {
					totalTime  = _totalTime,
					playTime   = _playTime,
					editorTime = _editorTime
				};

				var json = JsonConvert.SerializeObject(stats, Formatting.Indented);
				File.WriteAllText(_currentSessionFile, json);
			} catch (Exception ex) {
				Logger.LogError($"Error saving current session: {ex.Message}");
			}
		}

		private void SaveToHistory() {
			try {
				var currentStats = new TimeStatistics {
					totalTime  = _totalTime,
					playTime   = _playTime,
					editorTime = _editorTime
				};

				var history = new List<TimeStatistics>();

				// Charger l'historique existant
				if (File.Exists(_historyFile)) {
					var existingJson    = File.ReadAllText(_historyFile);
					var existingHistory = JsonConvert.DeserializeObject<List<TimeStatistics>>(existingJson);
					if (existingHistory != null) {
						history = existingHistory;
					}
				}

				// Ajouter la session actuelle à l'historique
				history.Add(currentStats);

				// Garder seulement les 100 dernières sessions pour éviter que le fichier devienne trop gros
				if (history.Count > 100) {
					history.RemoveRange(0, history.Count - 100);
				}

				// Sauvegarder l'historique mis à jour
				var json = JsonConvert.SerializeObject(history, Formatting.Indented);
				File.WriteAllText(_historyFile, json);

				Logger.Log($"Session saved to history: {TimeStatistics.FormatTime(currentStats.totalTime)}");
			} catch (Exception ex) {
				Logger.LogError($"Error saving to history: {ex.Message}");
			}
		}

		private static bool IsFromToday(DateTime date) {
			return date.Date == DateTime.Now.Date;
		}

		public List<TimeStatistics> GetHistory() {
			try {
				if (File.Exists(_historyFile)) {
					var json = File.ReadAllText(_historyFile);
					return JsonConvert.DeserializeObject<List<TimeStatistics>>(json) ?? new List<TimeStatistics>();
				}
			} catch (Exception ex) {
				Logger.LogError($"Error loading history: {ex.Message}");
			}

			return new List<TimeStatistics>();
		}

		public string GetSaveDirectory()
			=> _saveDirectory;

		public void Dispose() {
			if (_isDisposed) return;

			SaveCurrentSession();
			SaveToHistory();

			try {
				if (File.Exists(_currentSessionFile)) {
					File.Delete(_currentSessionFile);
				}
			} catch (Exception ex) {
				Logger.LogError($"Error cleaning up current session file: {ex.Message}");
			}

			_isDisposed    = true;
			_isInitialized = false;

			Logger.Log("PlayTimeTracker disposed and session saved to history");
		}

		private string GetFormattedTotalTime()
			=> TimeStatistics.FormatTime(_totalTime);

		private string GetFormattedPlayTime()
			=> TimeStatistics.FormatTime(_playTime);

		private string GetFormattedEditorTime()
			=> TimeStatistics.FormatTime(_editorTime);
	}
}