using UnityEditor;
using UnityEngine;

namespace Hactazia.FFPlay.Editor {
	[CustomEditor(typeof(SubtitleWorker))]
	public class SubtitlePlayerEditor : UnityEditor.Editor {
		private SerializedProperty onDisplayProp;
		private SerializedProperty onClearProp;
		private SerializedProperty currentTextProp;
		private SerializedProperty currentTimeProp;

		private bool showEvents = false;
		private bool showRuntimeInfo = true;
		private Vector2 scrollPosition;

		private void OnEnable() {
			onDisplayProp   = serializedObject.FindProperty(nameof(SubtitleWorker.onDisplay));
			onClearProp     = serializedObject.FindProperty(nameof(SubtitleWorker.onClear));
			currentTextProp = serializedObject.FindProperty(nameof(SubtitleWorker.currentText));
			currentTimeProp = serializedObject.FindProperty(nameof(SubtitleWorker.currentTime));
		}

		public override void OnInspectorGUI() {
			var player = (SubtitleWorker)target;
			serializedObject.Update();

			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("Subtitle Player", EditorStyles.boldLabel);
			EditorGUILayout.Space(5);

			// Events
			showEvents = EditorGUILayout.Foldout(showEvents, "Events", true);
			if (showEvents) {
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(onDisplayProp, new GUIContent("On Display"));
				EditorGUILayout.PropertyField(onClearProp, new GUIContent("On Clear"));
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.Space(10);

			// Runtime information
			if (Application.isPlaying) {
				showRuntimeInfo = EditorGUILayout.Foldout(showRuntimeInfo, "Current Subtitle", true);
				if (showRuntimeInfo) {
					EditorGUI.indentLevel++;

					// Display current subtitle text
					EditorGUILayout.LabelField("Text:", EditorStyles.boldLabel);
					
					if (!string.IsNullOrEmpty(player.currentText)) {
						// Create a styled text area for subtitle display
						GUIStyle textStyle = new GUIStyle(EditorStyles.helpBox);
						textStyle.fontSize = 12;
						textStyle.wordWrap = true;
						textStyle.padding = new RectOffset(10, 10, 10, 10);
						textStyle.normal.textColor = Color.white;

						scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(100));
						EditorGUILayout.TextArea(player.currentText, textStyle);
						EditorGUILayout.EndScrollView();
					} else {
						EditorGUILayout.HelpBox("No subtitle currently displayed", MessageType.None);
					}

					EditorGUILayout.Space(5);

					// Display timing information
					EditorGUI.BeginDisabledGroup(true);
					if (player.currentTime != null && player.currentTime.Length >= 2) {
						EditorGUILayout.DoubleField("Start Time (s)", player.currentTime[0]);
						EditorGUILayout.DoubleField("End Time (s)", player.currentTime[1]);
						
						double duration = player.currentTime[1] - player.currentTime[0];
						EditorGUILayout.DoubleField("Duration (s)", duration);
					}

					EditorGUILayout.LongField("PTS", player.pts);
					EditorGUI.EndDisabledGroup();

					EditorGUI.indentLevel--;
				}

				// Auto-repaint during playback
				Repaint();
			} else {
				EditorGUILayout.HelpBox("Subtitle information available in Play Mode", MessageType.Info);
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
