using Hactazia.FFPlay.Core;
using UnityEditor;
using UnityEngine;

namespace Hactazia.FFPlay.Editor {
	[CustomEditor(typeof(Player))]
	public class PlayerEditor : UnityEditor.Editor {
		private bool   showTimings       = true;
		private bool   showDebugInfo     = false;
		private bool   showStreamTimings = false;
		private string seekTime          = "0";

		public override void OnInspectorGUI() {
			var player = (Player)target;
			serializedObject.Update();

			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("FFPlay Unity Player V2", EditorStyles.boldLabel);
			EditorGUILayout.Space(5);

			// Worker references
			base.OnInspectorGUI();
			EditorGUILayout.Space(10);


			// Playback status
			EditorGUILayout.LabelField("Playback Status", EditorStyles.boldLabel);
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.Toggle("Is Playing", player.IsPlaying);
			EditorGUILayout.Toggle("Is Paused", player.IsPaused);
			EditorGUILayout.Toggle("Is Stream", player.IsStream);
			EditorGUILayout.Toggle("Is Stalled", player.IsStalled);
			EditorGUILayout.Toggle("Is Looping", player.IsLooping);
			EditorGUILayout.EnumPopup("Play State", player.CurrentPlayState);
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

				if (GUILayout.Button("Stop", GUILayout.Height(30))) {
					player.Stop();
				}

				EditorGUILayout.EndHorizontal();

				// Looping toggle
				var newLooping = EditorGUILayout.Toggle("Loop", player.IsLooping);
				if (newLooping != player.IsLooping)
					player.IsLooping = newLooping;

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

					// Progress bar
					var length = player.GetLength();
					if (length > 0) {
						var progress = (float)(player.PlaybackTime / length);
						EditorGUILayout.Space(5);
						var newProgress = EditorGUILayout.Slider("Progress", progress, 0f, 1f);
						if (Mathf.Abs(newProgress - progress) > 0.01f) {
							player.Seek(newProgress * length);
						}
					}
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

				// IStreamTimings Debug Info
				showStreamTimings = EditorGUILayout.Foldout(showStreamTimings, "Stream Timings Debug", true);
				if (showStreamTimings && player.Timings != null) {
					EditorGUI.indentLevel++;
					EditorGUI.BeginDisabledGroup(true);

					var timings = player.Timings;
					EditorGUILayout.Toggle("Is Valid", timings.IsValid);
					EditorGUILayout.Toggle("Is End", timings.IsEnd);
					EditorGUILayout.DoubleField("Start Time", timings.StartTime);
					EditorGUILayout.DoubleField("Length", timings.Length);

					EditorGUILayout.Space(5);
					EditorGUILayout.LabelField("Stream Status", EditorStyles.miniBoldLabel);
					foreach (var mediaType in System.Enum.GetValues(typeof(MediaType))) {
						var mt = (MediaType)mediaType;
						EditorGUILayout.Toggle($"Has {mt}", timings.Has(mt));
					}

					EditorGUILayout.Space(5);

					// Discontinuity info (if MultiStreamTimings)
					if (timings is MultiStreamTimings multiTimings) {
						EditorGUILayout.Space(5);
						EditorGUILayout.LabelField("Discontinuity", EditorStyles.miniBoldLabel);
						EditorGUILayout.Toggle("Correction Enabled", multiTimings.DiscontinuityCorrectionEnabled);
						EditorGUILayout.DoubleField("Threshold", multiTimings.DiscontinuityThreshold);

						foreach (var mediaType in System.Enum.GetValues(typeof(MediaType))) {
							var mt = (MediaType)mediaType;
							if (!timings.Has(mt)) continue;
							EditorGUILayout.DoubleField($"{mt} Last Discontinuity", multiTimings.GetDiscontinuityOffset(mt));
						}

						EditorGUILayout.Space(5);
						EditorGUILayout.LabelField("Decoders", EditorStyles.miniBoldLabel);

						foreach (var mediaType in System.Enum.GetValues(typeof(MediaType))) {
							var mt = (MediaType)mediaType;
							if (!timings.Has(mt)) continue;
							var decoder = timings.Get(mt);
							EditorGUILayout.LabelField($"{mt} Decoder", $"Stream {decoder.StreamIndex}, TimeBase: {decoder.TimeBase:F6}");
							EditorGUILayout.LabelField($"{mt} Pending", $"{timings.GetPendingSize(mt)}");
						}
					}

					EditorGUI.EndDisabledGroup();
					EditorGUI.indentLevel--;
				} else if (showStreamTimings && player.Timings == null) {
					EditorGUILayout.HelpBox("No timing system available", MessageType.Info);
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