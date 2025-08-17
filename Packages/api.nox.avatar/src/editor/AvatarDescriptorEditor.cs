#if UNITY_EDITOR
using Nox.CCK.Avatars;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace api.nox.avatar.editor {
	[CustomEditor(typeof(AvatarDescriptor))]
	public class AvatarDescriptorEditor : UnityEditor.Editor {
		private VisualElement _root;
		private Vector3Field  _viewPosition;

		private bool     _viewPositionEditing;
		private Button   _viewPositionEdit;

		private AvatarDescriptor descriptor
			=> target as AvatarDescriptor;

		private static bool TransformIsInAvatar(AvatarDescriptor descriptor, Transform t)
			=> descriptor && t.IsChildOf(descriptor.transform);

		private static Animator GetAnimator(AvatarDescriptor descriptor)
			=> descriptor.GetComponent<Animator>();

		public override VisualElement CreateInspectorGUI() {
			if (_root != null) return _root;
			_root = Resources.Load<VisualTreeAsset>("AvatarDescriptor").CloneTree();
			if (!descriptor) return _root;

			_viewPosition       = _root.Q<Vector3Field>("view-position");
			_viewPositionEdit   = _root.Q<Button>("view-position-edit");
			_viewPosition.value = descriptor.voicePosition;
			_viewPosition.RegisterValueChangedCallback(OnViewPositionChanged);
			_viewPositionEdit.RegisterCallback<ClickEvent>(OnViewPositionEditClick);

			UpdateViewPosition();

			return _root;
		}

		private void OnViewPositionChanged(ChangeEvent<Vector3> evt) {
			if (!descriptor) return;
			descriptor.viewPosition = evt.newValue;
			EditorUtility.SetDirty(descriptor);
		}

		private void UpdateViewPosition() {
			if (!_viewPositionEditing) {
				_viewPositionEdit.text = "Edit";
				return;
			}

			_viewPositionEdit.text = "Done";
			_viewPosition.value    = descriptor.viewPosition;
		}

		private void OnViewPositionEditClick(ClickEvent evt) {
			if (!descriptor) return;
			_viewPositionEditing = !_viewPositionEditing;
			UpdateViewPosition();
			SceneView.RepaintAll();
		}

		private void OnSceneGUI() {
			if (!descriptor) return;
			var worldViewPosition = descriptor.transform.TransformPoint(descriptor.viewPosition);
			Handles.color = Color.orangeRed;
			var forwardDirection = descriptor.transform.forward;
			var conePosition     = worldViewPosition + forwardDirection * 0.1f;
			var coneRotation     = Quaternion.LookRotation(forwardDirection);
			var coneSize         = 0.01f;
			Handles.DrawLine(worldViewPosition, conePosition);
			Handles.DrawWireDisc(worldViewPosition, forwardDirection, coneSize);
			Handles.ConeHandleCap(0, conePosition, coneRotation, coneSize, EventType.Repaint);
			if (!_viewPositionEditing) return;
			EditorGUI.BeginChangeCheck();
			var newWorldPosition = Handles.PositionHandle(worldViewPosition, Quaternion.identity);
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(descriptor, "Change View Position");
				descriptor.viewPosition = descriptor.transform.InverseTransformPoint(newWorldPosition);
				_viewPosition.SetValueWithoutNotify(descriptor.viewPosition);
				EditorUtility.SetDirty(descriptor);
			}
			Handles.Label(worldViewPosition + Vector3.up * 0.1f, "View Position", EditorStyles.boldLabel);
		}
	}
}
#endif