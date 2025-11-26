using UnityEditor;
using UnityEngine;
using FFmpeg.Unity;

namespace Hactazia.FFPlay.Editor {
	[CustomEditor(typeof(AudioWorker))]
	public class AudioPlayerEditor : UnityEditor.Editor {
		private SerializedProperty bufferSizeProp;
		private bool               showRuntimeInfo = true;
		private bool               showEventInfo   = false;

		private void OnEnable() {
			bufferSizeProp = serializedObject.FindProperty(nameof(AudioWorker.bufferSize));
		}

		public override void OnInspectorGUI() {
			var player = (AudioWorker)target;
			serializedObject.Update();

			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("Audio Player", EditorStyles.boldLabel);
			EditorGUILayout.Space(5);

			// Settings
			EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(bufferSizeProp, new GUIContent("Buffer Size (s)", "Audio buffer duration in seconds"));

			if (bufferSizeProp.floatValue < 0.1f) {
				bufferSizeProp.floatValue = 0.1f;
			}

			EditorGUILayout.Space(10);

			// Runtime information
			if (Application.isPlaying) {
				showRuntimeInfo = EditorGUILayout.Foldout(showRuntimeInfo, "Runtime Information", true);
				if (showRuntimeInfo) {
					EditorGUI.indentLevel++;
					EditorGUI.BeginDisabledGroup(true);

					// Reflection to access private fields
					var channelsField = typeof(AudioWorker).GetField(
						"channels",
						System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
					);
					var frequencyField = typeof(AudioWorker).GetField(
						"frequency",
						System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
					);
					var sampleFormatField = typeof(AudioWorker).GetField(
						"sampleFormat",
						System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
					);

					if (channelsField != null) {
						EditorGUILayout.IntField("Channels", (int)channelsField.GetValue(player));
					}

					if (frequencyField != null) {
						EditorGUILayout.IntField("Frequency", (int)frequencyField.GetValue(player));
					}

					if (sampleFormatField != null) {
						var format = sampleFormatField.GetValue(player);
						EditorGUILayout.TextField("Sample Format", format?.ToString() ?? "None");
					}

					EditorGUILayout.LongField("PTS", player.pts);

					EditorGUI.EndDisabledGroup();
					EditorGUI.indentLevel--;
				}

				// Event information
				showEventInfo = EditorGUILayout.Foldout(showEventInfo, "Event Subscribers", true);
				if (showEventInfo) {
					EditorGUI.indentLevel++;
					EditorGUI.BeginDisabledGroup(true);

					// Use reflection to check event subscribers
					var onResumeField = typeof(AudioWorker).GetField(
						"OnResume",
						System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
					);
					var onPauseField = typeof(AudioWorker).GetField(
						"OnPause",
						System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
					);
					var onSeekField = typeof(AudioWorker).GetField(
						"OnSeek",
						System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
					);
					var onVolumeChangeField = typeof(AudioWorker).GetField(
						"OnVolumeChange",
						System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
					);
					var addQueueField = typeof(AudioWorker).GetField(
						"AddQueue",
						System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
					);

					EditorGUILayout.IntField("OnResume", GetDelegateCount(onResumeField?.GetValue(player) as System.Delegate));
					EditorGUILayout.IntField("OnPause", GetDelegateCount(onPauseField?.GetValue(player) as System.Delegate));
					EditorGUILayout.IntField("OnSeek", GetDelegateCount(onSeekField?.GetValue(player) as System.Delegate));
					EditorGUILayout.IntField("OnVolumeChange", GetDelegateCount(onVolumeChangeField?.GetValue(player) as System.Delegate));
					EditorGUILayout.IntField("AddQueue", GetDelegateCount(addQueueField?.GetValue(player) as System.Delegate));

					EditorGUI.EndDisabledGroup();
					EditorGUI.indentLevel--;
				}

				// Auto-repaint during playback
				Repaint();
			} else {
				EditorGUILayout.HelpBox("Runtime information available in Play Mode", MessageType.Info);
			}

			serializedObject.ApplyModifiedProperties();
		}

		private int GetDelegateCount(System.Delegate del) {
			return del?.GetInvocationList()?.Length ?? 0;
		}
	}
}