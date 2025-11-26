using UnityEditor;
using UnityEngine;

namespace Hactazia.FFPlay.Editor {
	[CustomEditor(typeof(Player))]
	public class PlayerEditor : UnityEditor.Editor {
		private SerializedProperty videoPlayerProp;
		private SerializedProperty audioPlayerProp;
		private SerializedProperty subtitlePlayerProp;
		private SerializedProperty videoOffsetProp;
		private SerializedProperty audioOffsetProp;
		private SerializedProperty subtitleOffsetProp;

		private bool   showTimings   = true;
		private bool   showDebugInfo = false;
		private string seekTime      = "0";

		private void OnEnable() {
			videoPlayerProp    = serializedObject.FindProperty(nameof(Player.videoWorker));
			audioPlayerProp    = serializedObject.FindProperty(nameof(Player.audioWorker));
			subtitlePlayerProp = serializedObject.FindProperty(nameof(Player.subtitleWorker));
			videoOffsetProp    = serializedObject.FindProperty(nameof(Player.videoOffset));
			audioOffsetProp    = serializedObject.FindProperty(nameof(Player.audioOffset));
			subtitleOffsetProp = serializedObject.FindProperty(nameof(Player.subtitleOffset));
		}

		public override void OnInspectorGUI() {
			var player = (Player)target;
			serializedObject.Update();

			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("FFPlay Unity Player", EditorStyles.boldLabel);
			EditorGUILayout.Space(5);

			// Player references
			EditorGUILayout.PropertyField(videoPlayerProp, new GUIContent("Video Player"));
			EditorGUILayout.PropertyField(audioPlayerProp, new GUIContent("Audio Player"));
			EditorGUILayout.PropertyField(subtitlePlayerProp, new GUIContent("Subtitle Player"));

			EditorGUILayout.Space(10);

			// Sync offsets
			EditorGUILayout.LabelField("Synchronization", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(videoOffsetProp, new GUIContent("Video Offset (s)"));
			EditorGUILayout.PropertyField(audioOffsetProp, new GUIContent("Audio Offset (s)"));
			EditorGUILayout.PropertyField(subtitleOffsetProp, new GUIContent("Subtitle Offset (s)"));

			EditorGUILayout.Space(10);

			// Playback status
			EditorGUILayout.LabelField("Playback Status", EditorStyles.boldLabel);
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.Toggle("Is Playing", player.IsPlaying);
			EditorGUILayout.Toggle("Is Paused", player.IsPaused);
			EditorGUILayout.Toggle("Is Stream", player.IsStream);
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.Space(10);

			// Playback controls
			if (Application.isPlaying) {
				EditorGUILayout.LabelField("Playback Controls", EditorStyles.boldLabel);

				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button(player.IsPaused ? "Resume" : "Pause", GUILayout.Height(30))) {
					if (player.IsPaused)
						player.Resume();
					else
						player.Pause();
				}

				EditorGUILayout.EndHorizontal();

				if (!player.IsStream) {
					EditorGUILayout.Space(5);
					EditorGUILayout.BeginHorizontal();
					seekTime = EditorGUILayout.TextField("Seek Time (s)", seekTime);
					if (GUILayout.Button("Seek", GUILayout.Width(60))) {
						if (double.TryParse(seekTime, out double time)) {
							player.Seek(time);
						}
					}

					EditorGUILayout.EndHorizontal();
				}

				EditorGUILayout.Space(10);

				// Timing information
				showTimings = EditorGUILayout.Foldout(showTimings, "Timing Information", true);
				if (showTimings) {
					EditorGUI.indentLevel++;
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.DoubleField("Playback Time", player.PlaybackTime);
					EditorGUILayout.DoubleField("Video Time", player.VideoTime);
					EditorGUILayout.DoubleField("Audio Time", player.AudioTime);
					EditorGUILayout.DoubleField("Subtitle Time", player.SubtitleTime);
					EditorGUILayout.DoubleField("Length", player.GetLength());
					EditorGUI.EndDisabledGroup();
					EditorGUI.indentLevel--;
				}

				// Debug information
				showDebugInfo = EditorGUILayout.Foldout(showDebugInfo, "Debug Information", true);
				if (showDebugInfo) {
					EditorGUI.indentLevel++;
					EditorGUI.BeginDisabledGroup(true);

					foreach (var timing in player.GetTimings()) {
						if (timing == null) continue;
						EditorGUILayout.LabelField(timing.GetType().Name, EditorStyles.boldLabel);
						EditorGUILayout.Toggle("Is Input Valid", timing.IsInputValid);
						EditorGUILayout.Toggle("End of File", timing.IsEndOfFile);
						EditorGUILayout.DoubleField("Start Time", timing.StartTime);
						EditorGUILayout.DoubleField("Time Base Seconds", timing.TimeBaseSeconds);
						EditorGUILayout.DoubleField("Current PTS", timing.GetCurrentFrame().pts);
						EditorGUILayout.Space(5);
					}

					EditorGUI.EndDisabledGroup();
					EditorGUI.indentLevel--;
				}

				// Auto-repaint during playback
				if (player.IsPlaying)
					Repaint();
			} else {
				EditorGUILayout.HelpBox("Playback controls available in Play Mode", MessageType.Info);
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}