using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEngine;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.statistics {
	public class Main : MainModInitializer {
		private PlayTimeTracker _playTimeTracker;

		public void OnInitializeMain(MainModCoreAPI api) {
			Logger.LogDebug("abcd");
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
		public DateTime SessionDate         { get; set; }
		public float    TotalTime           { get; set; }
		public float    PlayTime            { get; set; }
		public float    EditorTime          { get; set; }
		public string   FormattedTotalTime  { get; set; }
		public string   FormattedPlayTime   { get; set; }
		public string   FormattedEditorTime { get; set; }

		public TimeStatistics() {
			SessionDate = DateTime.Now;
		}
	}

	public class PlayTimeTracker : IDisposable {
		// Variables de temps en secondes
		public float TotalTime  { get; private set; }
		public float PlayTime   { get; private set; }
		public float EditorTime { get; private set; }

		private float _sessionStartTime;
		private float _lastUpdateTime;
		private float _lastSaveTime;
		private bool  _isInitialized;
		private bool  _isDisposed;

		// Paramètres de sauvegarde
		private const    float  SaveInterval = 30f; // Sauvegarde toutes les 30 secondes
		private readonly string _saveDirectory;
		private readonly string _currentSessionFile;
		private readonly string _historyFile;

		public PlayTimeTracker() {
			_saveDirectory      = Path.Combine(Application.persistentDataPath, "NoxStatistics");
			_currentSessionFile = Path.Combine(_saveDirectory, "current_session.json");
			_historyFile        = Path.Combine(_saveDirectory, "time_history.json");
		}

		public void Initialize() {
			if (_isInitialized) return;

			// Créer le dossier de sauvegarde s'il n'existe pas
			if (!Directory.Exists(_saveDirectory)) {
				Directory.CreateDirectory(_saveDirectory);
			}

			// Charger les données de la session précédente si elles existent
			LoadCurrentSession();

			_sessionStartTime = Time.realtimeSinceStartup;
			_lastUpdateTime   = _sessionStartTime;
			_lastSaveTime     = _sessionStartTime;
			_isInitialized    = true;

			Debug.Log($"PlayTimeTracker initialized. Save directory: {_saveDirectory}");
		}

		public void Update() {
			if (!_isInitialized || _isDisposed) return;

			float currentTime = Time.realtimeSinceStartup;
			float deltaTime   = currentTime - _lastUpdateTime;

			// Met à jour le temps total
			TotalTime += deltaTime;

			// Détermine si on est en mode éditeur ou en jeu
			if (Application.isEditor)
				EditorTime += deltaTime;

			if (Application.isPlaying)
				PlayTime += deltaTime;

			_lastUpdateTime = currentTime;

			// Sauvegarde périodique
			if (currentTime - _lastSaveTime >= SaveInterval) {
				SaveCurrentSession();
				_lastSaveTime = currentTime;
			}
		}

		private void LoadCurrentSession() {
			try {
				if (File.Exists(_currentSessionFile)) {
					string json  = File.ReadAllText(_currentSessionFile);
					var    stats = JsonConvert.DeserializeObject<TimeStatistics>(json);

					if (stats != null && IsFromToday(stats.SessionDate)) {
						TotalTime  = stats.TotalTime;
						PlayTime   = stats.PlayTime;
						EditorTime = stats.EditorTime;
						Debug.Log($"Loaded current session: Total={GetFormattedTotalTime()}, Play={GetFormattedPlayTime()}, Editor={GetFormattedEditorTime()}");
					}
				}
			} catch (Exception ex) {
				Debug.LogError($"Error loading current session: {ex.Message}");
			}
		}

		private void SaveCurrentSession() {
			try {
				var stats = new TimeStatistics {
					TotalTime           = TotalTime,
					PlayTime            = PlayTime,
					EditorTime          = EditorTime,
					FormattedTotalTime  = GetFormattedTotalTime(),
					FormattedPlayTime   = GetFormattedPlayTime(),
					FormattedEditorTime = GetFormattedEditorTime()
				};

				string json = JsonConvert.SerializeObject(stats, Formatting.Indented);
				File.WriteAllText(_currentSessionFile, json);
			} catch (Exception ex) {
				Debug.LogError($"Error saving current session: {ex.Message}");
			}
		}

		private void SaveToHistory() {
			try {
				var currentStats = new TimeStatistics {
					TotalTime           = TotalTime,
					PlayTime            = PlayTime,
					EditorTime          = EditorTime,
					FormattedTotalTime  = GetFormattedTotalTime(),
					FormattedPlayTime   = GetFormattedPlayTime(),
					FormattedEditorTime = GetFormattedEditorTime()
				};

				List<TimeStatistics> history = new List<TimeStatistics>();

				// Charger l'historique existant
				if (File.Exists(_historyFile)) {
					string existingJson    = File.ReadAllText(_historyFile);
					var    existingHistory = JsonConvert.DeserializeObject<List<TimeStatistics>>(existingJson);
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
				string json = JsonConvert.SerializeObject(history, Formatting.Indented);
				File.WriteAllText(_historyFile, json);

				Debug.Log($"Session saved to history: {currentStats.FormattedTotalTime} total time");
			} catch (Exception ex) {
				Debug.LogError($"Error saving to history: {ex.Message}");
			}
		}

		private bool IsFromToday(DateTime date) {
			return date.Date == DateTime.Now.Date;
		}

		public List<TimeStatistics> GetHistory() {
			try {
				if (File.Exists(_historyFile)) {
					string json = File.ReadAllText(_historyFile);
					return JsonConvert.DeserializeObject<List<TimeStatistics>>(json) ?? new List<TimeStatistics>();
				}
			} catch (Exception ex) {
				Debug.LogError($"Error loading history: {ex.Message}");
			}

			return new List<TimeStatistics>();
		}

		public string GetSaveDirectory() {
			return _saveDirectory;
		}

		public void Dispose() {
			if (_isDisposed) return;

			// Sauvegarder une dernière fois avant de disposer
			SaveCurrentSession();
			SaveToHistory();

			// Nettoyer le fichier de session courante
			try {
				if (File.Exists(_currentSessionFile)) {
					File.Delete(_currentSessionFile);
				}
			} catch (Exception ex) {
				Debug.LogError($"Error cleaning up current session file: {ex.Message}");
			}

			_isDisposed    = true;
			_isInitialized = false;

			Debug.Log("PlayTimeTracker disposed and session saved to history");
		}

		// Méthodes utilitaires pour obtenir les temps formatés
		public string GetFormattedTotalTime() {
			return FormatTime(TotalTime);
		}

		public string GetFormattedPlayTime() {
			return FormatTime(PlayTime);
		}

		public string GetFormattedEditorTime() {
			return FormatTime(EditorTime);
		}

		private string FormatTime(float seconds) {
			TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
			return timeSpan.ToString(@"hh\:mm\:ss");
		}
	}
}