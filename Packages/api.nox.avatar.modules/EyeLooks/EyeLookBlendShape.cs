using System;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
#endif

namespace Nox.CCK.Avatars.EyeLooks {
	[Serializable, Tooltip("Blend Shape")]
	public class EyeLookBlendShape : BaseEyeLook {
		public SkinnedMeshRenderer mesh;
		public string[]            shapes = { "", "", "", "" };

		public override void UpdateEye(EyeLookContext context, Vector4 direction) {
			var indexDown  = mesh.sharedMesh.GetBlendShapeIndex(shapes[1]);
			var indexUp    = mesh.sharedMesh.GetBlendShapeIndex(shapes[0]);
			var indexLeft  = mesh.sharedMesh.GetBlendShapeIndex(shapes[2]);
			var indexRight = mesh.sharedMesh.GetBlendShapeIndex(shapes[3]);
			mesh.SetBlendShapeWeight(indexDown, direction.x  / -limits.x * 100);
			mesh.SetBlendShapeWeight(indexUp, direction.y    / limits.y  * 100);
			mesh.SetBlendShapeWeight(indexLeft, direction.z  / -limits.z * 100);
			mesh.SetBlendShapeWeight(indexRight, direction.w / limits.w  * 100);
		}

		#if UNITY_EDITOR
		public override VisualElement CreateInspectorGUI(EyeLookContext context) {
			// Charger le fichier UXML spécifique à EyeLookBlendShape
			var visualTree = Resources.Load<VisualTreeAsset>("EyeLookBlendShape");
			if (!visualTree) {
				Debug.LogError("Could not load EyeLookBlendShape.uxml from Resources folder");
				return base.CreateInspectorGUI(context);
			}
			var container = visualTree.CloneTree();
			var limitVerticalSlider = container.Q<MinMaxSlider>("limit-vertical");
			var limitHorizontalSlider = container.Q<MinMaxSlider>("limit-horizontal");
			var minLimitVerticalField = container.Q<FloatField>("min-limit-vertical");
			var maxLimitVerticalField = container.Q<FloatField>("max-limit-vertical");
			var minLimitHorizontalField = container.Q<FloatField>("min-limit-horizontal");
			var maxLimitHorizontalField = container.Q<FloatField>("max-limit-horizontal");
			var meshField       = container.Q<ObjectField>("mesh-field");
			var shapeUpField    = container.Q<DropdownField>("shape-up");
			var shapeDownField  = container.Q<DropdownField>("shape-down");
			var shapeLeftField  = container.Q<DropdownField>("shape-left");
			var shapeRightField = container.Q<DropdownField>("shape-right");
			if (limitVerticalSlider != null && minLimitVerticalField != null && maxLimitVerticalField != null) {
				limitVerticalSlider.value = new Vector2(limits.x, limits.y);
				minLimitVerticalField.value = limits.x;
				maxLimitVerticalField.value = limits.y;
				limitVerticalSlider.RegisterValueChangedCallback(evt => {
					limits = new Vector4(evt.newValue.x, evt.newValue.y, limits.z, limits.w);
					minLimitVerticalField.SetValueWithoutNotify(evt.newValue.x);
					maxLimitVerticalField.SetValueWithoutNotify(evt.newValue.y);
				});
				minLimitVerticalField.RegisterValueChangedCallback(evt => {
					limits = new Vector4(evt.newValue, limits.y, limits.z, limits.w);
					limitVerticalSlider.SetValueWithoutNotify(new Vector2(evt.newValue, limits.y));
				});
				maxLimitVerticalField.RegisterValueChangedCallback(evt => {
					limits = new Vector4(limits.x, evt.newValue, limits.z, limits.w);
					limitVerticalSlider.SetValueWithoutNotify(new Vector2(limits.x, evt.newValue));
				});
			}
			
			if (limitHorizontalSlider != null && minLimitHorizontalField != null && maxLimitHorizontalField != null) {
				limitHorizontalSlider.value = new Vector2(limits.z, limits.w);
				minLimitHorizontalField.value = limits.z;
				maxLimitHorizontalField.value = limits.w;
				limitHorizontalSlider.RegisterValueChangedCallback(evt => {
					limits = new Vector4(limits.x, limits.y, evt.newValue.x, evt.newValue.y);
					minLimitHorizontalField.SetValueWithoutNotify(evt.newValue.x);
					maxLimitHorizontalField.SetValueWithoutNotify(evt.newValue.y);
				});
				minLimitHorizontalField.RegisterValueChangedCallback(evt => {
					limits = new Vector4(limits.x, limits.y, evt.newValue, limits.w);
					limitHorizontalSlider.SetValueWithoutNotify(new Vector2(evt.newValue, limits.w));
				});
				maxLimitHorizontalField.RegisterValueChangedCallback(evt => {
					limits = new Vector4(limits.x, limits.y, limits.z, evt.newValue);
					limitHorizontalSlider.SetValueWithoutNotify(new Vector2(limits.z, evt.newValue));
				});
			}
			if (meshField != null) {
				meshField.value = mesh;
				meshField.RegisterValueChangedCallback(
					evt => {
						mesh = evt.newValue as SkinnedMeshRenderer;
						UpdateBlendShapeDropdowns(new[] { shapeUpField, shapeDownField, shapeLeftField, shapeRightField });
					}
				);
			}
			var shapeFields = new[] { shapeUpField, shapeDownField, shapeLeftField, shapeRightField };
			for (int i = 0; i < shapeFields.Length; i++) {
				var field = shapeFields[i];
				if (field != null) {
					int shapeIndex = i; // Capture pour la closure
					field.RegisterValueChangedCallback(
						evt => {
							if (shapes is not { Length: 4 })
								shapes = new string[4];
							shapes[shapeIndex] = evt.newValue ?? "";
						}
					);
				}
			}
			UpdateBlendShapeDropdowns(shapeFields);
			return container;
		}

		private void UpdateBlendShapeDropdowns(DropdownField[] dropdownFields) {
			var blendShapeNames = new List<string> { string.Empty };
			if (mesh && mesh.sharedMesh)
				for (var i = 0; i < mesh.sharedMesh.blendShapeCount; i++)
					blendShapeNames.Add(mesh.sharedMesh.GetBlendShapeName(i));
			for (var i = 0; i < dropdownFields.Length; i++) {
				var field = dropdownFields[i];
				if (field == null) continue;
				field.choices = blendShapeNames;
				field.formatListItemCallback = (value) => string.IsNullOrEmpty(value) ? "None" : value;
				field.formatSelectedValueCallback = (value) => string.IsNullOrEmpty(value) ? "None" : value;
				var currentValue = i < shapes.Length ? shapes[i] : "";
				field.value = blendShapeNames.Contains(currentValue)
					? currentValue
					: string.Empty;
			}
		}
		#endif
	}
}