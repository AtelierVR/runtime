using UnityEditor;
using UnityEngine;

namespace Hactazia.FFPlay.Editor {
	[CustomEditor(typeof(BufferAudioSource))]
	public class BufferAudioSourceEditor : UnityEditor.Editor {
		private SerializedProperty audiowProp;
		private SerializedProperty audioSourceProp;

		private bool showRuntimeInfo = true;

		private void OnEnable() {
			audiowProp      = serializedObject.FindProperty(nameof(BufferAudioSource.worker));
			audioSourceProp = serializedObject.FindProperty(nameof(BufferAudioSource.audioSource));
		}

		public override void OnInspectorGUI() {
			var buffer = (BufferAudioSource)target;
			serializedObject.Update();

			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("Buffer Audio Source", EditorStyles.boldLabel);
			EditorGUILayout.Space(5);

			// References
			EditorGUILayout.PropertyField(audiowProp, new GUIContent("Audio Worker"));
			EditorGUILayout.PropertyField(audioSourceProp, new GUIContent("Audio Source"));

			EditorGUILayout.Space(10);

			// Runtime information
			if (Application.isPlaying) {
				showRuntimeInfo = EditorGUILayout.Foldout(showRuntimeInfo, "Runtime Information", true);
				if (showRuntimeInfo) {
					EditorGUI.indentLevel++;
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.Toggle("Is Playing", buffer.audioSource.isPlaying);
					EditorGUILayout.FloatField("Volume", buffer.audioSource.volume);
					EditorGUILayout.FloatField("Pitch", buffer.audioSource.pitch);
					EditorGUI.EndDisabledGroup();
					EditorGUI.indentLevel--;
				}
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}