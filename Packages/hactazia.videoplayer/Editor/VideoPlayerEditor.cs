using UnityEngine;
using UnityEditor;
using Logger = Nox.CCK.Utils.Logger;

namespace Hactazia.VideoPlayer.Editor {
	[CustomEditor(typeof(VideoPlayer))]
	public class VideoPlayerEditor : UnityEditor.Editor {
		public override void OnInspectorGUI() {
			DrawDefaultInspector();

			var videoPlayer = (VideoPlayer)target;

			// Section Load URL
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Load Video", EditorStyles.boldLabel);

			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("Enter video URL:", EditorStyles.label);
			videoPlayer.videoUrl = EditorGUILayout.TextField(videoPlayer.videoUrl);

			EditorGUILayout.BeginHorizontal();
			GUI.enabled = !string.IsNullOrEmpty(videoPlayer.videoUrl) && Application.isPlaying;
			if (GUILayout.Button("Load URL"))
				try {
					if (videoPlayer.LoadVideo(videoPlayer.videoUrl)) {
						Logger.Log($"VideoPlayer: Successfully loaded video from URL: {videoPlayer.videoUrl}");
					} else {
						Logger.LogError($"VideoPlayer: Failed to load video from URL: {videoPlayer.videoUrl}");
					}
				} catch (System.Exception e) {
					Logger.LogError($"VideoPlayer: Error loading video - {e.Message}");
				}


			GUI.enabled = !string.IsNullOrEmpty(videoPlayer.videoUrl);
			if (GUILayout.Button("Clear")) 
				videoPlayer.videoUrl = "";

			GUI.enabled = true;
			EditorGUILayout.EndHorizontal();

			if (!Application.isPlaying) {
				EditorGUILayout.HelpBox("Enter Play Mode to load videos", MessageType.Info);
			}

			EditorGUILayout.EndVertical();

			// Section Debug Info
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Debug Information", EditorStyles.boldLabel);

			using (new EditorGUI.DisabledGroupScope(true)) {
				EditorGUILayout.BeginVertical("box");

				// État de la vidéo avec les nouvelles propriétés
				EditorGUILayout.LabelField("Player State", videoPlayer.State.ToString());
				EditorGUILayout.LabelField("Video State", $"Loaded: {videoPlayer.IsLoaded}, Playing: {videoPlayer.IsPlaying}, Paused: {videoPlayer.IsPaused}, Stopped: {videoPlayer.IsStopped}");
				EditorGUILayout.LabelField("Player ID", videoPlayer.PlayerId >= 0 ? videoPlayer.PlayerId.ToString() : "Not Initialized");

				// Affichage des erreurs si présentes
				if (videoPlayer.HasError) {
					EditorGUILayout.Space();
					EditorGUILayout.LabelField("Error", EditorStyles.boldLabel);
					EditorGUILayout.LabelField("Error Type", videoPlayer.Error.ToString());
					EditorGUILayout.LabelField("Error Message", videoPlayer.ErrorMessage, EditorStyles.wordWrappedLabel);
				}

				if (videoPlayer.IsLoaded) {
					// Dimensions utilisant les propriétés du VideoPlayer
					EditorGUILayout.LabelField("Dimensions", $"{videoPlayer.VideoWidth} x {videoPlayer.VideoHeight}");

					// Durée et temps actuel utilisant les propriétés du VideoPlayer
					EditorGUILayout.LabelField("Duration", $"{FormatTime(videoPlayer.Duration)}");
					EditorGUILayout.LabelField("Current Time", $"{FormatTime(videoPlayer.CurrentTime)}");

					// Progression
					float progress = videoPlayer.Duration > 0 ? (float)(videoPlayer.CurrentTime / videoPlayer.Duration) : 0f;
					EditorGUILayout.LabelField("Progress", $"{progress:P1} ({progress * 100:F1}%)");
					EditorGUILayout.Slider("Progress Bar", progress, 0f, 1f);

					// Frame Rate utilisant la propriété du VideoPlayer
					EditorGUILayout.LabelField("Frame Rate", $"{videoPlayer.FrameRate:F2} fps");

					// Texture de sortie
					if (videoPlayer.OutputTexture != null) {
						EditorGUILayout.LabelField("Output Texture", $"{videoPlayer.OutputTexture.width}x{videoPlayer.OutputTexture.height} ({videoPlayer.OutputTexture.format})");
					} else {
						EditorGUILayout.LabelField("Output Texture", "None");
					}
				} else {
					EditorGUILayout.LabelField("Video not loaded", EditorStyles.centeredGreyMiniLabel);
				}

				EditorGUILayout.EndVertical();
			}

			// Repaint automatique quand la vidéo joue
			if (Application.isPlaying && videoPlayer.IsPlaying) {
				Repaint();
			}

			// Section Controls (seulement en mode Play)
			if (Application.isPlaying) {
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Video Controls", EditorStyles.boldLabel);

				EditorGUILayout.BeginHorizontal();

				// Bouton Play/Resume
				GUI.enabled = videoPlayer.IsLoaded && (!videoPlayer.IsPlaying || videoPlayer.IsPaused);
				if (GUILayout.Button(videoPlayer.IsPaused ? "Resume" : "Play")) {
					if (videoPlayer.IsPaused) {
						videoPlayer.Resume();
					} else {
						// Add safety checks before calling Play()
						try {
							if (videoPlayer != null && videoPlayer.IsLoaded && !videoPlayer.HasError) {
								videoPlayer.Play();
							} else {
								Debug.LogWarning("VideoPlayer: Cannot play - player not properly initialized or has errors");
							}
						} catch (System.Exception e) {
							Debug.LogError($"VideoPlayer: Error calling Play() - {e.Message}");
						}
					}
				}

				// Bouton Pause
				GUI.enabled = videoPlayer.IsLoaded && videoPlayer.IsPlaying && !videoPlayer.IsPaused;
				if (GUILayout.Button("Pause")) {
					videoPlayer.Pause();
				}

				// Bouton Stop
				GUI.enabled = videoPlayer.IsLoaded && (videoPlayer.IsPlaying || videoPlayer.IsPaused);
				if (GUILayout.Button("Stop")) {
					videoPlayer.Stop();
				}

				GUI.enabled = true; // Réactiver l'interface
				EditorGUILayout.EndHorizontal();

				// Section Seek (nouvelle fonctionnalité)
				if (videoPlayer.IsLoaded) {
					EditorGUILayout.Space();
					EditorGUILayout.LabelField("Seek Controls", EditorStyles.boldLabel);

					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Seek to time:", GUILayout.Width(80));

					float seekTime = EditorGUILayout.Slider((float)videoPlayer.CurrentTime, 0f, (float)videoPlayer.Duration);

					if (GUI.changed && !videoPlayer.IsPlaying) {
						videoPlayer.Seek(seekTime);
					}

					EditorGUILayout.EndHorizontal();
				}
			}
		}

		private string FormatTime(double timeInSeconds) {
			if (double.IsNaN(timeInSeconds) || double.IsInfinity(timeInSeconds)) {
				return "00:00";
			}

			var timeSpan = System.TimeSpan.FromSeconds(timeInSeconds);
			if (timeSpan.TotalHours >= 1) {
				return timeSpan.ToString(@"hh\:mm\:ss");
			}

			return timeSpan.ToString(@"mm\:ss");
		}
	}
}