using UnityEditor;
using UnityEngine;
using Hactazia.FFPlay;

namespace Hactazia.FFPlay.Editor {
	[CustomEditor(typeof(VideoWorker))]
	public class VideoWorkerEditor : UnityEditor.Editor {
		private SerializedProperty flipTextureProp;
		private SerializedProperty imageProp;
		private SerializedProperty dimsProp;

		private bool showEvents         = false;
		private bool showRuntimeInfo    = true;
		private bool showTexturePreview = true;

		private void OnEnable() {
			flipTextureProp = serializedObject.FindProperty(nameof(VideoWorker.flipTexture));
			imageProp       = serializedObject.FindProperty(nameof(VideoWorker.image));
			dimsProp        = serializedObject.FindProperty(nameof(VideoWorker.dims));
		}

		public override void OnInspectorGUI() {
			var player = (VideoWorker)target;
			serializedObject.Update();

			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("Video Player", EditorStyles.boldLabel);
			EditorGUILayout.Space(5);

			// Settings
			EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(flipTextureProp, new GUIContent("Flip Texture", "Flip texture on Y axis (minor performance cost)"));

			EditorGUILayout.Space(10);

			// Events
			showEvents = EditorGUILayout.Foldout(showEvents, "Events", true);
			if (showEvents) {
				EditorGUI.indentLevel++;

				EditorGUI.BeginDisabledGroup(true);
				// Use reflection to get event subscriber counts
				var onDisplayField = typeof(VideoPlayer).GetField(
					"OnDisplay",
					System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
				);
				var onResizeField = typeof(VideoPlayer).GetField(
					"OnResize",
					System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
				);

				var onDisplayEvent = onDisplayField?.GetValue(player);
				var onResizeEvent  = onResizeField?.GetValue(player);

				EditorGUILayout.IntField("OnDisplay Subscribers", GetUnityEventCount(onDisplayEvent));
				EditorGUILayout.IntField("OnResize Subscribers", GetUnityEventCount(onResizeEvent));
				EditorGUI.EndDisabledGroup();

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.Space(10);

			// Runtime information
			if (Application.isPlaying) {
				showRuntimeInfo = EditorGUILayout.Foldout(showRuntimeInfo, "Runtime Information", true);
				if (showRuntimeInfo) {
					EditorGUI.indentLevel++;
					EditorGUI.BeginDisabledGroup(true);

					EditorGUILayout.PropertyField(dimsProp, new GUIContent("Dimensions"));
					EditorGUILayout.LongField("PTS", player.pts);

					if (player.image != null) {
						EditorGUILayout.IntField("Texture Width", player.image.width);
						EditorGUILayout.IntField("Texture Height", player.image.height);
						EditorGUILayout.TextField("Format", player.image.format.ToString());
						EditorGUILayout.IntField("Mipmap Count", player.image.mipmapCount);
					}

					EditorGUI.EndDisabledGroup();
					EditorGUI.indentLevel--;
				}

				// Texture preview
				if (player.image != null) {
					EditorGUILayout.Space(5);
					showTexturePreview = EditorGUILayout.Foldout(showTexturePreview, "Texture Preview", true);
					if (showTexturePreview) {
						EditorGUI.indentLevel++;

						// Calculate preview size maintaining aspect ratio
						float maxWidth    = EditorGUIUtility.currentViewWidth - 50;
						float maxHeight   = 300;
						float aspectRatio = (float)player.image.width / player.image.height;

						float previewWidth  = maxWidth;
						float previewHeight = previewWidth / aspectRatio;

						if (previewHeight > maxHeight) {
							previewHeight = maxHeight;
							previewWidth  = previewHeight * aspectRatio;
						}

						Rect previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);
						EditorGUI.DrawPreviewTexture(previewRect, player.image, null, ScaleMode.ScaleToFit);

						EditorGUI.indentLevel--;
					}
				}

				// Auto-repaint during playback
				Repaint();
			} else {
				EditorGUILayout.HelpBox("Runtime information available in Play Mode", MessageType.Info);
			}

			serializedObject.ApplyModifiedProperties();
		}

		private int GetUnityEventCount(object unityEvent) {
			if (unityEvent == null) return 0;

			var persistentCallsProperty = unityEvent.GetType().GetProperty("PersistentCallCount");
			if (persistentCallsProperty != null) {
				return (int)persistentCallsProperty.GetValue(unityEvent);
			}

			return 0;
		}
	}
}