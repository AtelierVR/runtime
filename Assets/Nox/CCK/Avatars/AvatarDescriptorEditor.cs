#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars {
	[CustomEditor(typeof(AvatarDescriptor))]
	public class AvatarDescriptorEditor : Editor {
		public override bool UseDefaultMargins()
			=> false;

		private VisualElement root;

		private bool    editingViewPosition;
		private bool    editingVoicePosition;
		private Vector2 previewEyeLook = new(0f, 0f);

		private static bool TransformIsInAvatar(AvatarDescriptor descriptor, Transform t)
			=> descriptor && t.IsChildOf(descriptor.transform);

		private static Animator GetAnimator(AvatarDescriptor descriptor)
			=> descriptor.GetComponent<Animator>();

		private static Avatar GetAvatar(AvatarDescriptor descriptor) {
			var animator = GetAnimator(descriptor);
			return animator ? animator.avatar : null;
		}

		public override VisualElement CreateInspectorGUI() {
			if (root != null)
				return root;

			root = Resources.Load<VisualTreeAsset>($"api.nox.cck.avatar.avatar_descriptor").CloneTree();
			var descriptor = target as AvatarDescriptor;
			if (!descriptor) return root;

			var viewPosition = root.Q<Vector3Field>("view-position");
			viewPosition.value = descriptor.viewPosition;
			viewPosition.RegisterValueChangedCallback(
				e => {
					descriptor.viewPosition = e.newValue;
					EditorUtility.SetDirty(target);
				}
			);

			var voicePosition = root.Q<Vector3Field>("voice-position");
			voicePosition.value = descriptor.voicePosition;
			voicePosition.RegisterValueChangedCallback(
				e => {
					descriptor.voicePosition = e.newValue;
					EditorUtility.SetDirty(target);
				}
			);

			var voiceParent = root.Q<ObjectField>("voice-parent");
			voiceParent.value = descriptor.voiceParent;
			voiceParent.RegisterValueChangedCallback(
				e => {
					if (e.newValue is not Transform t || !TransformIsInAvatar(descriptor, t)) {
						Debug.LogWarning("Voice parent must be a child of the avatar");
						return;
					}

					descriptor.voiceParent = t;
					EditorUtility.SetDirty(target);
				}
			);

			var viewPositionEdit  = root.Q<Button>("view-position-edit");
			var viewPositionClose = root.Q<Button>("view-position-close");

			viewPositionEdit.clicked += () => {
				if (editingViewPosition) return;
				viewPositionEdit.style.display  = DisplayStyle.None;
				viewPositionClose.style.display = DisplayStyle.Flex;
				editingViewPosition             = true;
			};

			viewPositionClose.clicked += () => {
				if (!editingViewPosition) return;
				viewPositionEdit.style.display  = DisplayStyle.Flex;
				viewPositionClose.style.display = DisplayStyle.None;
				editingViewPosition             = false;
			};

			viewPositionEdit.style.display  = editingViewPosition ? DisplayStyle.None : DisplayStyle.Flex;
			viewPositionClose.style.display = editingViewPosition ? DisplayStyle.Flex : DisplayStyle.None;

			var voicePositionEdit  = root.Q<Button>("voice-position-edit");
			var voicePositionClose = root.Q<Button>("voice-position-close");

			voicePositionEdit.clicked += () => {
				if (editingVoicePosition) return;
				voicePositionEdit.style.display  = DisplayStyle.None;
				voicePositionClose.style.display = DisplayStyle.Flex;
				editingVoicePosition             = true;
			};

			voicePositionClose.clicked += () => {
				if (!editingVoicePosition) return;
				voicePositionEdit.style.display  = DisplayStyle.Flex;
				voicePositionClose.style.display = DisplayStyle.None;
				editingVoicePosition             = false;
			};

			voicePositionEdit.style.display  = editingVoicePosition ? DisplayStyle.None : DisplayStyle.Flex;
			voicePositionClose.style.display = editingVoicePosition ? DisplayStyle.Flex : DisplayStyle.None;

			var voiceParentDetect = root.Q<Button>("voice-parent-detect");
			voiceParentDetect.clicked += () => {
				var animator = GetAnimator(descriptor);
				if (!animator) {
					EditorUtility.DisplayDialog("Error", "Avatar must have an Animator component", "OK");
					return;
				}

				var headBone = animator.GetBoneTransform(HumanBodyBones.Head);
				if (!headBone) {
					EditorUtility.DisplayDialog("Error", "Avatar must have a head bone", "OK");
					return;
				}

				descriptor.voiceParent = headBone;
				voiceParent.value      = headBone;
				EditorUtility.SetDirty(target);
			};

			var useEyeMovements = root.Q<Toggle>("use-eye-movements");
			useEyeMovements.value = descriptor.useEyeMovements;
			useEyeMovements.RegisterValueChangedCallback(
				e => {
					descriptor.useEyeMovements = e.newValue;
					EditorUtility.SetDirty(target);
				}
			);

			var eyeTargetMin    = root.Q<UnsignedIntegerField>("eye-target-min");
			var eyeTargetMax    = root.Q<UnsignedIntegerField>("eye-target-max");
			var eyeTargetSlider = root.Q<MinMaxSlider>("eye-target-slider");
			eyeTargetSlider.value = descriptor.eyeIntervalTarget;
			eyeTargetSlider.RegisterValueChangedCallback(
				e => {
					var v = new Vector2Int((int)e.newValue.x, (int)e.newValue.y);
					descriptor.eyeIntervalTarget = v;
					eyeTargetMin.SetValueWithoutNotify((uint)v.x);
					eyeTargetMax.SetValueWithoutNotify((uint)v.y);
					EditorUtility.SetDirty(target);
				}
			);

			eyeTargetMin.value = (uint)descriptor.eyeIntervalTarget.x;
			eyeTargetMin.RegisterValueChangedCallback(
				e => {
					descriptor.eyeIntervalTarget.x = (int)e.newValue;
					eyeTargetSlider.SetValueWithoutNotify(descriptor.eyeIntervalTarget);
					EditorUtility.SetDirty(target);
				}
			);

			eyeTargetMax.value = (uint)descriptor.eyeIntervalTarget.y;
			eyeTargetMax.RegisterValueChangedCallback(
				e => {
					descriptor.eyeIntervalTarget.y = (int)e.newValue;
					eyeTargetSlider.SetValueWithoutNotify(descriptor.eyeIntervalTarget);
					EditorUtility.SetDirty(target);
				}
			);

			var eyeLookType = root.Q<EnumField>("eye-look-type");
			eyeLookType.Init(descriptor.eyeLookType);
			eyeLookType.RegisterValueChangedCallback(
				e => {
					descriptor.eyeLookType = (AvatarDescriptor.EyeLookType)e.newValue;
					UpdateEyeList();
					UpdateEyeLookPreview();
					EditorUtility.SetDirty(target);
				}
			);

			UpdateEyeList();

			var faceMesh = root.Q<ObjectField>("face-mesh");
			faceMesh.value = descriptor.faceMesh;
			faceMesh.RegisterValueChangedCallback(
				e => {
					descriptor.faceMesh = e.newValue as SkinnedMeshRenderer;
					EditorUtility.SetDirty(target);
				}
			);

			var previewEyeY      = root.Q<Slider>("preview-eye-y");
			var previewEyeX      = root.Q<Slider>("preview-eye-x");
			var previewEyeYValue = root.Q<FloatField>("preview-eye-y-value");
			var previewEyeXValue = root.Q<FloatField>("preview-eye-x-value");
			var previewEyeReset  = root.Q<Button>("preview-eye-reset");

			previewEyeY.value      = previewEyeLook.y;
			previewEyeX.value      = previewEyeLook.x;
			previewEyeYValue.value = previewEyeLook.y;
			previewEyeXValue.value = previewEyeLook.x;

			previewEyeY.RegisterValueChangedCallback(
				e => {
					previewEyeLook.y = e.newValue;
					previewEyeYValue.SetValueWithoutNotify(previewEyeLook.y);
					UpdateEyeLookPreview();
					EditorUtility.SetDirty(target);
				}
			);

			previewEyeX.RegisterValueChangedCallback(
				e => {
					previewEyeLook.x = e.newValue;
					previewEyeXValue.SetValueWithoutNotify(previewEyeLook.x);
					UpdateEyeLookPreview();
					EditorUtility.SetDirty(target);
				}
			);

			previewEyeYValue.RegisterValueChangedCallback(
				e => {
					previewEyeLook.y = e.newValue;
					previewEyeY.SetValueWithoutNotify(previewEyeLook.y);
					UpdateEyeLookPreview();
					EditorUtility.SetDirty(target);
				}
			);

			previewEyeXValue.RegisterValueChangedCallback(
				e => {
					previewEyeLook.x = e.newValue;
					previewEyeX.SetValueWithoutNotify(previewEyeLook.x);
					UpdateEyeLookPreview();
					EditorUtility.SetDirty(target);
				}
			);

			previewEyeReset.clicked += () => {
				previewEyeLook = Vector2.zero;
				previewEyeY.SetValueWithoutNotify(0);
				previewEyeX.SetValueWithoutNotify(0);
				previewEyeYValue.SetValueWithoutNotify(0);
				previewEyeXValue.SetValueWithoutNotify(0);
				UpdateEyeLookPreview();
				EditorUtility.SetDirty(target);
			};

			return root;
		}

		private void UpdateEyeLookPreview() {
			var descriptor = target as AvatarDescriptor;
			if (!descriptor) return;

			var eyes = descriptor.eyeLooks;
			if (eyes == null || eyes.Length == 0) return;

			foreach (var eye in eyes) {
				var left = previewEyeLook.x < eye.angleLimits.z
					? -eye.angleLimits.z
					: previewEyeLook.x < 0
						? -previewEyeLook.x
						: 0;

				var right = previewEyeLook.x > eye.angleLimits.w
					? eye.angleLimits.w
					: previewEyeLook.x > 0
						? previewEyeLook.x
						: 0;

				var up = previewEyeLook.y > eye.angleLimits.y
					? eye.angleLimits.y
					: previewEyeLook.y > 0
						? previewEyeLook.y
						: 0;

				var down = previewEyeLook.y < eye.angleLimits.x
					? -eye.angleLimits.x
					: previewEyeLook.y < 0
						? -previewEyeLook.y
						: 0;

				if (descriptor.eyeLookType == AvatarDescriptor.EyeLookType.Muscle) {
					var animator = GetAnimator(descriptor);
					if (!animator) continue;

					animator.SetLookAtPosition(
						animator.transform.position + new Vector3(left, up, 0)
					);
					animator.SetLookAtWeight(
						1f,
						1f,
						1f,
						1f,
						0.5f
					);
				} else if (descriptor.eyeLookType == AvatarDescriptor.EyeLookType.Transform) {
					if (!eye.target) continue;

					eye.target.localRotation = Quaternion.Euler(
						down - up,
						right - left,
						0
					);
				} else if (descriptor.eyeLookType == AvatarDescriptor.EyeLookType.BlendShape) {
					var mesh = eye.mesh;
					if (!mesh) continue;

					var indexDown  = mesh.sharedMesh.GetBlendShapeIndex(eye.blendShapes[1]);
					var indexUp    = mesh.sharedMesh.GetBlendShapeIndex(eye.blendShapes[0]);
					var indexLeft  = mesh.sharedMesh.GetBlendShapeIndex(eye.blendShapes[2]);
					var indexRight = mesh.sharedMesh.GetBlendShapeIndex(eye.blendShapes[3]);

					mesh.SetBlendShapeWeight(indexDown, down   / -eye.angleLimits.x * 100);
					mesh.SetBlendShapeWeight(indexUp, up       / eye.angleLimits.y  * 100);
					mesh.SetBlendShapeWeight(indexLeft, left   / -eye.angleLimits.z * 100);
					mesh.SetBlendShapeWeight(indexRight, right / eye.angleLimits.w  * 100);
				}
			}
		}

		private void UpdateEyeList() {
			var eyes       = root.Q<ListView>("eyes");
			var descriptor = target as AvatarDescriptor;
			if (!descriptor) return;

			eyes.makeItem = () => {
				if (descriptor.eyeLookType == AvatarDescriptor.EyeLookType.Transform)
					return MakeEyeTransform(descriptor);
				if (descriptor.eyeLookType == AvatarDescriptor.EyeLookType.BlendShape)
					return MakeEyeBlendShape(descriptor);
				return new VisualElement();
			};

			eyes.bindItem = (e, i) => {
				if (descriptor.eyeLookType == AvatarDescriptor.EyeLookType.Transform)
					BindEyeTransform(descriptor, e, i);
				else if (descriptor.eyeLookType == AvatarDescriptor.EyeLookType.BlendShape)
					BindEyeBlendShape(descriptor, e, i);
			};

			eyes.onAdd = (e) => {
				var selectedIndex = e is ListView listView ? listView.selectedIndex : -1;
				var eye = selectedIndex >= 0 && selectedIndex < descriptor.eyeLooks.Length
					? descriptor.eyeLooks[selectedIndex]
					: descriptor.eyeLooks.Length == 0
						? new AvatarDescriptor.EyeLook { mesh = descriptor.faceMesh }
						: descriptor.eyeLooks[^1];
				var newEyes = descriptor.eyeLooks.ToList();
				newEyes.Add(eye);
				descriptor.eyeLooks = newEyes.ToArray();
				UpdateEyeList();
				EditorUtility.SetDirty(descriptor);
			};

			eyes.onRemove = (e) => {
				if (e is not ListView listView) return;
				if (listView.selectedIndex < 0 || listView.selectedIndex >= descriptor.eyeLooks.Length) return;
				var index   = listView.selectedIndex;
				var newEyes = descriptor.eyeLooks.ToList();
				newEyes.RemoveAt(index);
				descriptor.eyeLooks = newEyes.ToArray();
				UpdateEyeList();
				EditorUtility.SetDirty(descriptor);
			};

			eyes.itemsSource = descriptor.eyeLooks;
		}

		private void BindEyeBlendShape(AvatarDescriptor descriptor, VisualElement item, int index) {
			item.userData = index;

			var label = item.Q<Label>("label");
			label.text = $"Eye #{index + 1}";

			if (descriptor.eyeLooks.Length <= index) return;
			var eye = descriptor.eyeLooks[index];
			if (eye == null) return;

			var mesh = item.Q<ObjectField>("mesh");
			mesh.value = eye.mesh;

			var limitDown            = item.Q<FloatField>("limit-down");
			var limitUp              = item.Q<FloatField>("limit-up");
			var limitLeft            = item.Q<FloatField>("limit-left");
			var limitRight           = item.Q<FloatField>("limit-right");
			var limitUpDownSlider    = item.Q<MinMaxSlider>("limit-up_down-slider");
			var limitLeftRightSlider = item.Q<MinMaxSlider>("limit-left_right-slider");
			limitDown.value            = eye.angleLimits.x;
			limitUp.value              = eye.angleLimits.y;
			limitLeft.value            = eye.angleLimits.z;
			limitRight.value           = eye.angleLimits.w;
			limitUpDownSlider.value    = new Vector2(eye.angleLimits.x, eye.angleLimits.y);
			limitLeftRightSlider.value = new Vector2(eye.angleLimits.z, eye.angleLimits.w);

			UpdateEyeBlendShape(index, item);
		}

		private void UpdateEyeBlendShape(int index, VisualElement item) {
			var descriptor = target as AvatarDescriptor;
			if (!descriptor) return;

			if (descriptor.eyeLooks.Length <= index) return;
			var eye = descriptor.eyeLooks[index];
			if (eye == null) return;

			List<string> blendShapes = new() { "-none-" };
			if (eye.mesh) {
				var renderer = eye.mesh;
				for (var i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
					blendShapes.Add(renderer.sharedMesh.GetBlendShapeName(i));
			}

			var lookUp = item.Q<DropdownField>("look-up");
			lookUp.choices = blendShapes;
			lookUp.value = string.IsNullOrEmpty(eye.blendShapes[0]) || blendShapes.IndexOf(eye.blendShapes[0]) < 0
				? blendShapes[0]
				: eye.blendShapes[0];

			var lookDown = item.Q<DropdownField>("look-down");
			lookDown.choices = blendShapes;
			lookDown.value = string.IsNullOrEmpty(eye.blendShapes[1]) || blendShapes.IndexOf(eye.blendShapes[1]) < 0
				? blendShapes[0]
				: eye.blendShapes[1];

			var lookLeft = item.Q<DropdownField>("look-left");
			lookLeft.choices = blendShapes;
			lookLeft.value = string.IsNullOrEmpty(eye.blendShapes[2]) || blendShapes.IndexOf(eye.blendShapes[2]) < 0
				? blendShapes[0]
				: eye.blendShapes[2];

			var lookRight = item.Q<DropdownField>("look-right");
			lookRight.choices = blendShapes;
			lookRight.value = string.IsNullOrEmpty(eye.blendShapes[3]) || blendShapes.IndexOf(eye.blendShapes[3]) < 0
				? blendShapes[0]
				: eye.blendShapes[3];
		}

		private void BindEyeTransform(AvatarDescriptor descriptor, VisualElement item, int index) {
			item.userData = index;

			var label = item.Q<Label>("label");
			label.text = $"Eye #{index + 1}";

			if (descriptor.eyeLooks.Length <= index) return;
			var eye = descriptor.eyeLooks[index];
			if (eye == null) return;

			var objectField = item.Q<ObjectField>("target");
			objectField.value = eye.target;

			var placement = item.Q<EnumField>("placement");
			placement.Init(eye.placement);

			var limitDown            = item.Q<FloatField>("limit-down");
			var limitUp              = item.Q<FloatField>("limit-up");
			var limitLeft            = item.Q<FloatField>("limit-left");
			var limitRight           = item.Q<FloatField>("limit-right");
			var limitUpDownSlider    = item.Q<MinMaxSlider>("limit-up_down-slider");
			var limitLeftRightSlider = item.Q<MinMaxSlider>("limit-left_right-slider");
			limitDown.value            = eye.angleLimits.x;
			limitUp.value              = eye.angleLimits.y;
			limitLeft.value            = eye.angleLimits.z;
			limitRight.value           = eye.angleLimits.w;
			limitUpDownSlider.value    = new Vector2(eye.angleLimits.x, eye.angleLimits.y);
			limitLeftRightSlider.value = new Vector2(eye.angleLimits.z, eye.angleLimits.w);
		}

		private VisualElement MakeEyeBlendShape(AvatarDescriptor descriptor) {
			var item = Resources
				.Load<VisualTreeAsset>($"api.nox.cck.avatar.avatar_descriptor.eye_blend_shape")
				.CloneTree();

			var label = item.Q<Label>("label");
			label.text = "Eye #-";

			var placement = item.Q<EnumField>("placement");
			placement.Init(AvatarDescriptor.EyePlacement.Other);
			placement.RegisterValueChangedCallback(
				e => {
					if (item.userData is not (int i and >= 0) || i >= descriptor.eyeLooks.Length) return;
					descriptor.eyeLooks[i].placement = (AvatarDescriptor.EyePlacement)e.newValue;
					EditorUtility.SetDirty(descriptor);
				}
			);

			var objectField = item.Q<ObjectField>("mesh");
			objectField.RegisterValueChangedCallback(
				e => {
					if (item.userData is not (int i and >= 0) || i >= descriptor.eyeLooks.Length) return;
					descriptor.eyeLooks[i].mesh = e.newValue as SkinnedMeshRenderer;
					UpdateEyeBlendShape(i, item);
					EditorUtility.SetDirty(descriptor);
				}
			);

			var lookUp = item.Q<DropdownField>("look-up");
			lookUp.RegisterValueChangedCallback(
				e => {
					if (item.userData is not (int i and >= 0) || i >= descriptor.eyeLooks.Length) return;
					descriptor.eyeLooks[i].blendShapes[0] = e.newValue;
					EditorUtility.SetDirty(descriptor);
				}
			);

			var lookDown = item.Q<DropdownField>("look-down");
			lookDown.RegisterValueChangedCallback(
				e => {
					if (item.userData is not (int i and >= 0) || i >= descriptor.eyeLooks.Length) return;
					descriptor.eyeLooks[i].blendShapes[1] = e.newValue;
					EditorUtility.SetDirty(descriptor);
				}
			);

			var lookLeft = item.Q<DropdownField>("look-left");
			lookLeft.RegisterValueChangedCallback(
				e => {
					if (item.userData is not (int i and >= 0) || i >= descriptor.eyeLooks.Length) return;
					descriptor.eyeLooks[i].blendShapes[2] = e.newValue;
					EditorUtility.SetDirty(descriptor);
				}
			);

			var lookRight = item.Q<DropdownField>("look-right");
			lookRight.RegisterValueChangedCallback(
				e => {
					if (item.userData is not (int i and >= 0) || i >= descriptor.eyeLooks.Length) return;
					descriptor.eyeLooks[i].blendShapes[3] = e.newValue;
					EditorUtility.SetDirty(descriptor);
				}
			);

			var limitUp              = item.Q<FloatField>("limit-up");
			var limitDown            = item.Q<FloatField>("limit-down");
			var limitLeft            = item.Q<FloatField>("limit-left");
			var limitRight           = item.Q<FloatField>("limit-right");
			var limitUpDownSlider    = item.Q<MinMaxSlider>("limit-up_down-slider");
			var limitLeftRightSlider = item.Q<MinMaxSlider>("limit-left_right-slider");

			limitUp.RegisterValueChangedCallback(
				e => {
					if (item.userData is not (int i and >= 0) || i >= descriptor.eyeLooks.Length) return;
					descriptor.eyeLooks[i].angleLimits.y = e.newValue;
					limitUpDownSlider.SetValueWithoutNotify(
						new Vector2(
							descriptor.eyeLooks[i].angleLimits.x,
							descriptor.eyeLooks[i].angleLimits.y
						)
					);
					EditorUtility.SetDirty(descriptor);
				}
			);

			limitDown.RegisterValueChangedCallback(
				e => {
					if (item.userData is not (int i and >= 0) || i >= descriptor.eyeLooks.Length) return;
					descriptor.eyeLooks[i].angleLimits.x = e.newValue;
					limitUpDownSlider.SetValueWithoutNotify(
						new Vector2(
							descriptor.eyeLooks[i].angleLimits.x,
							descriptor.eyeLooks[i].angleLimits.y
						)
					);
					EditorUtility.SetDirty(descriptor);
				}
			);

			limitLeft.RegisterValueChangedCallback(
				e => {
					if (item.userData is not (int i and >= 0) || i >= descriptor.eyeLooks.Length) return;
					descriptor.eyeLooks[i].angleLimits.z = e.newValue;
					limitLeftRightSlider.SetValueWithoutNotify(
						new Vector2(
							descriptor.eyeLooks[i].angleLimits.z,
							descriptor.eyeLooks[i].angleLimits.w
						)
					);
					EditorUtility.SetDirty(descriptor);
				}
			);

			limitRight.RegisterValueChangedCallback(
				e => {
					if (item.userData is not (int i and >= 0) || i >= descriptor.eyeLooks.Length) return;
					descriptor.eyeLooks[i].angleLimits.w = e.newValue;
					limitLeftRightSlider.SetValueWithoutNotify(
						new Vector2(
							descriptor.eyeLooks[i].angleLimits.z,
							descriptor.eyeLooks[i].angleLimits.w
						)
					);
					EditorUtility.SetDirty(descriptor);
				}
			);

			limitUpDownSlider.RegisterValueChangedCallback(
				e => {
					if (item.userData is not (int i and >= 0) || i >= descriptor.eyeLooks.Length) return;
					descriptor.eyeLooks[i].angleLimits.x = e.newValue.x;
					descriptor.eyeLooks[i].angleLimits.y = e.newValue.y;
					limitDown.SetValueWithoutNotify(e.newValue.x);
					limitUp.SetValueWithoutNotify(e.newValue.y);
					EditorUtility.SetDirty(descriptor);
				}
			);

			limitLeftRightSlider.RegisterValueChangedCallback(
				e => {
					if (item.userData is not (int i and >= 0) || i >= descriptor.eyeLooks.Length) return;
					descriptor.eyeLooks[i].angleLimits.z = e.newValue.x;
					descriptor.eyeLooks[i].angleLimits.w = e.newValue.y;
					limitLeft.SetValueWithoutNotify(e.newValue.x);
					limitRight.SetValueWithoutNotify(e.newValue.y);
					EditorUtility.SetDirty(descriptor);
				}
			);

			return item;
		}

		private VisualElement MakeEyeTransform(AvatarDescriptor descriptor) {
			var item = Resources
				.Load<VisualTreeAsset>($"api.nox.cck.avatar.avatar_descriptor.eye_angle")
				.CloneTree();

			var label = item.Q<Label>("label");
			label.text = "Eye #-";

			var placement = item.Q<EnumField>("placement");
			placement.Init(AvatarDescriptor.EyePlacement.Other);
			placement.RegisterValueChangedCallback(
				e => {
					if (item.userData is not (int i and >= 0) || i >= descriptor.eyeLooks.Length) return;
					descriptor.eyeLooks[i].placement = (AvatarDescriptor.EyePlacement)e.newValue;
					EditorUtility.SetDirty(descriptor);
				}
			);

			var objectField = item.Q<ObjectField>("target");
			objectField.objectType = typeof(Transform);
			objectField.RegisterValueChangedCallback(
				e => {
					if (item.userData is not (int i and >= 0) || i >= descriptor.eyeLooks.Length) return;
					descriptor.eyeLooks[i].target = e.newValue as Transform;
					EditorUtility.SetDirty(descriptor);
				}
			);

			var targetDetect = item.Q<Button>("target-detect");
			targetDetect.clicked += () => {
				if (item.userData is not (int i and >= 0) || i >= descriptor.eyeLooks.Length) return;

				if (descriptor.eyeLooks[i].placement == AvatarDescriptor.EyePlacement.Left) {
					var animator = GetAnimator(descriptor);
					if (!animator) {
						EditorUtility.DisplayDialog("Error", "Avatar must have an Animator component", "OK");
						return;
					}

					descriptor.eyeLooks[i].target = animator.GetBoneTransform(HumanBodyBones.LeftEye);
					objectField.value             = descriptor.eyeLooks[i].target;

					EditorUtility.SetDirty(descriptor);
				} else if (descriptor.eyeLooks[i].placement == AvatarDescriptor.EyePlacement.Right) {
					var animator = GetAnimator(descriptor);
					if (!animator) {
						EditorUtility.DisplayDialog("Error", "Avatar must have an Animator component", "OK");
						return;
					}

					descriptor.eyeLooks[i].target = animator.GetBoneTransform(HumanBodyBones.RightEye);
					objectField.value             = descriptor.eyeLooks[i].target;

					EditorUtility.SetDirty(descriptor);
				} else EditorUtility.DisplayDialog("Error", "Eye placement must be Left or Right to detect", "OK");
			};

			var limitUp              = item.Q<FloatField>("limit-up");
			var limitDown            = item.Q<FloatField>("limit-down");
			var limitLeft            = item.Q<FloatField>("limit-left");
			var limitRight           = item.Q<FloatField>("limit-right");
			var limitUpDownSlider    = item.Q<MinMaxSlider>("limit-up_down-slider");
			var limitLeftRightSlider = item.Q<MinMaxSlider>("limit-left_right-slider");

			limitUp.RegisterValueChangedCallback(
				e => {
					if (item.userData is not (int i and >= 0) || i >= descriptor.eyeLooks.Length) return;
					descriptor.eyeLooks[i].angleLimits.y = e.newValue;
					limitUpDownSlider.SetValueWithoutNotify(
						new Vector2(
							descriptor.eyeLooks[i].angleLimits.x,
							descriptor.eyeLooks[i].angleLimits.y
						)
					);
					UpdateEyeLookPreview();
					EditorUtility.SetDirty(descriptor);
				}
			);

			limitDown.RegisterValueChangedCallback(
				e => {
					if (item.userData is not (int i and >= 0) || i >= descriptor.eyeLooks.Length) return;
					descriptor.eyeLooks[i].angleLimits.x = e.newValue;
					limitUpDownSlider.SetValueWithoutNotify(
						new Vector2(
							descriptor.eyeLooks[i].angleLimits.x,
							descriptor.eyeLooks[i].angleLimits.y
						)
					);
					UpdateEyeLookPreview();
					EditorUtility.SetDirty(descriptor);
				}
			);

			limitLeft.RegisterValueChangedCallback(
				e => {
					if (item.userData is not (int i and >= 0) || i >= descriptor.eyeLooks.Length) return;
					descriptor.eyeLooks[i].angleLimits.z = e.newValue;
					limitLeftRightSlider.SetValueWithoutNotify(
						new Vector2(
							descriptor.eyeLooks[i].angleLimits.z,
							descriptor.eyeLooks[i].angleLimits.w
						)
					);
					UpdateEyeLookPreview();
					EditorUtility.SetDirty(descriptor);
				}
			);

			limitRight.RegisterValueChangedCallback(
				e => {
					if (item.userData is not (int i and >= 0) || i >= descriptor.eyeLooks.Length) return;
					descriptor.eyeLooks[i].angleLimits.w = e.newValue;
					limitLeftRightSlider.SetValueWithoutNotify(
						new Vector2(
							descriptor.eyeLooks[i].angleLimits.z,
							descriptor.eyeLooks[i].angleLimits.w
						)
					);
					UpdateEyeLookPreview();
					EditorUtility.SetDirty(descriptor);
				}
			);

			limitUpDownSlider.RegisterValueChangedCallback(
				e => {
					if (item.userData is not (int i and >= 0) || i >= descriptor.eyeLooks.Length) return;
					descriptor.eyeLooks[i].angleLimits.x = e.newValue.x;
					descriptor.eyeLooks[i].angleLimits.y = e.newValue.y;
					limitDown.SetValueWithoutNotify(e.newValue.x);
					limitUp.SetValueWithoutNotify(e.newValue.y);
					UpdateEyeLookPreview();
					EditorUtility.SetDirty(descriptor);
				}
			);

			limitLeftRightSlider.RegisterValueChangedCallback(
				e => {
					if (item.userData is not (int i and >= 0) || i >= descriptor.eyeLooks.Length) return;
					descriptor.eyeLooks[i].angleLimits.z = e.newValue.x;
					descriptor.eyeLooks[i].angleLimits.w = e.newValue.y;
					limitLeft.SetValueWithoutNotify(e.newValue.x);
					limitRight.SetValueWithoutNotify(e.newValue.y);
					UpdateEyeLookPreview();
					EditorUtility.SetDirty(descriptor);
				}
			);

			return item;
		}

		private void OnSceneGUI() {
			var descriptor = target as AvatarDescriptor;
			if (!descriptor) return;

			var viewPosition  = descriptor.transform.TransformPoint(descriptor.viewPosition);
			var voicePosition = descriptor.transform.TransformPoint(descriptor.voicePosition);

			Handles.color = Color.green;
			Handles.DrawWireDisc(viewPosition, Vector3.up, 0.01f);
			Handles.DrawWireDisc(viewPosition, Vector3.right, 0.01f);
			Handles.DrawWireDisc(viewPosition, Vector3.forward, 0.01f);

			Handles.color = Color.blue;
			Handles.DrawWireDisc(voicePosition, Vector3.up, 0.01f);
			Handles.DrawWireDisc(voicePosition, Vector3.right, 0.01f);
			Handles.DrawWireDisc(voicePosition, Vector3.forward, 0.01f);

			if (editingViewPosition) {
				EditorGUI.BeginChangeCheck();
				var newPosition = Handles.PositionHandle(descriptor.viewPosition, Quaternion.identity);
				if (EditorGUI.EndChangeCheck()) {
					Undo.RecordObject(descriptor, "Change View Position");
					descriptor.viewPosition                     = descriptor.transform.InverseTransformPoint(newPosition);
					root.Q<Vector3Field>("view-position").value = descriptor.viewPosition;
					EditorUtility.SetDirty(target);
				}
			}

			if (editingVoicePosition) {
				EditorGUI.BeginChangeCheck();
				var newPosition = Handles.PositionHandle(descriptor.voicePosition, Quaternion.identity);
				if (EditorGUI.EndChangeCheck()) {
					Undo.RecordObject(descriptor, "Change Voice Position");
					descriptor.voicePosition                     = descriptor.transform.InverseTransformPoint(newPosition);
					root.Q<Vector3Field>("voice-position").value = descriptor.voicePosition;
					EditorUtility.SetDirty(target);
				}
			}

			if (descriptor.eyeLookType == AvatarDescriptor.EyeLookType.BlendShape) {
				var eyes = descriptor.eyeLooks;
				if (eyes == null || eyes.Length == 0) return;

				foreach (var eye in eyes) {
					var eyeTarget = eye.placement switch {
						AvatarDescriptor.EyePlacement.Left   => GetAnimator(descriptor).GetBoneTransform(HumanBodyBones.LeftEye).position,
						AvatarDescriptor.EyePlacement.Right  => GetAnimator(descriptor).GetBoneTransform(HumanBodyBones.RightEye).position,
						AvatarDescriptor.EyePlacement.Center => descriptor.viewPosition,
						AvatarDescriptor.EyePlacement.Other  => eye.mesh?.rootBone.position ?? Vector3.zero,
						_                                    => eye.target.position
					};

					Handles.color = Color.green;
					var rotation = Quaternion.Euler(-previewEyeLook.y, previewEyeLook.x, 0);
					var forward  = rotation * Vector3.forward;
					var position = eyeTarget + forward * 0.1f;
					Handles.DrawLine(eyeTarget, position);
				}
			} else if (descriptor.eyeLookType == AvatarDescriptor.EyeLookType.Transform) {
				var eyes = descriptor.eyeLooks;
				if (eyes == null || eyes.Length == 0) return;

				foreach (var eye in eyes) {
					var eyeTarget = eye.target;
					if (!eyeTarget) continue;
					
					var eyePosition = eyeTarget.position;
					Handles.color = Color.green;
					var rotation = Quaternion.Euler(-previewEyeLook.y, previewEyeLook.x, 0);
					var forward  = rotation * Vector3.forward;
					var position = eyePosition + forward * 0.1f;
					Handles.DrawLine(eyePosition, position);
					Handles.DrawWireDisc(eyePosition, forward, 0.01f);
					Handles.DrawWireDisc(eyePosition, Vector3.up, 0.01f);
					Handles.DrawWireDisc(eyePosition, Vector3.right, 0.01f);
					Handles.DrawWireDisc(eyePosition, Vector3.forward, 0.01f);
					Handles.Label(eyePosition, eyeTarget.name);
					Handles.color = Color.white;
					
				}
			}
		}
	}
}
#endif