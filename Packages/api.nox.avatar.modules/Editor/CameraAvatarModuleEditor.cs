#if UNITY_EDITOR
using Nox.Avatars;
using Nox.CCK.Avatars.Camera;
using UnityEditor;
using UnityEngine;

namespace api.nox.avatar.modules {
	[CustomEditor(typeof(CameraAvatarModule))]
	public class CameraAvatarModuleEditor : Editor {
		private CameraAvatarModule module
			=> (CameraAvatarModule)target;

		private bool isEditingOffset = false;

		public override void OnInspectorGUI() {
			DrawHeadTransformField();
			DrawOffsetField();
			
			serializedObject.ApplyModifiedProperties();
		}

		// ReSharper disable Unity.PerformanceAnalysis
		private IAvatarDescriptor GetDescriptor()
			=> module.GetComponentInParent<IAvatarDescriptor>();

		private void DrawHeadTransformField() {
			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.PropertyField(serializedObject.FindProperty("headTransform"));

			if (GUILayout.Button("Detect", GUILayout.Width(64))) {
				var head = GetDescriptor()
					.GetAnimator()
					?.GetBoneTransform(HumanBodyBones.Head);
				serializedObject.FindProperty("headTransform").objectReferenceValue = head;
			}

			EditorGUILayout.EndHorizontal();
		}

		private void DrawOffsetField() {
			EditorGUILayout.BeginHorizontal();
			
			// Show the property field
			EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraOffset"));
			
			// Toggle button for editing mode
			string buttonText = isEditingOffset ? "Done" : "Edit";
			if (GUILayout.Button(buttonText, GUILayout.Width(64))) {
				isEditingOffset = !isEditingOffset;
				SceneView.RepaintAll();
			}

			EditorGUILayout.EndHorizontal();
		}

		private void OnSceneGUI() {
			if (!isEditingOffset || !module) return;

			// Get the head transform position as the base
			var basePosition = module.headTransform 
				? module.headTransform.position 
				: module.transform.position;

			// Draw and handle the offset gizmo
			EditorGUI.BeginChangeCheck();
			var newOffset = Handles.PositionHandle(basePosition + module.cameraOffset, Quaternion.identity) - basePosition;
			
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(module, "Change Camera Offset");
				module.cameraOffset = newOffset;
				EditorUtility.SetDirty(module);
			}

			// Draw a line from base position to offset position
			Handles.color = Color.cyan;
			Handles.DrawLine(basePosition, basePosition + module.cameraOffset);
			
			// Draw labels
			Handles.Label(basePosition, "Head Position");
			Handles.Label(basePosition + module.cameraOffset, "Camera Position");
		}
	}
}
#endif