using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
#endif

namespace Nox.CCK.Avatars.EyeLooks {
	[Serializable, Tooltip("Transform")]
	public class EyeLookTransform : BaseEyeLook {
		public Transform target;

		public override void UpdateEye(EyeLookContext context, Vector4 direction)
			=> target.localRotation = Quaternion.Euler(
				direction.x - direction.y,
				direction.z - direction.w,
				0
			);

		#if UNITY_EDITOR
		public override VisualElement CreateInspectorGUI(EyeLookContext context) {
			// Charger le fichier UXML spécifique à EyeLookTransform
			var visualTree = Resources.Load<VisualTreeAsset>("EyeLookTransform");
			if (!visualTree) {
				Debug.LogError("Could not load EyeLookTransform.uxml from Resources folder");
				return base.CreateInspectorGUI(context); // Fallback vers l'interface de base
			}

			var container = visualTree.CloneTree();

			// Récupérer les éléments UI pour les limites
			var limitVerticalSlider     = container.Q<MinMaxSlider>("limit-vertical");
			var limitHorizontalSlider   = container.Q<MinMaxSlider>("limit-horizontal");
			var minLimitVerticalField   = container.Q<FloatField>("min-limit-vertical");
			var maxLimitVerticalField   = container.Q<FloatField>("max-limit-vertical");
			var minLimitHorizontalField = container.Q<FloatField>("min-limit-horizontal");
			var maxLimitHorizontalField = container.Q<FloatField>("max-limit-horizontal");

			// Récupérer l'élément UI pour le Transform
			var targetField = container.Q<ObjectField>("target-field");

			// Configurer les MinMaxSliders et FloatFields des limites
			if (limitVerticalSlider != null && minLimitVerticalField != null && maxLimitVerticalField != null) {
				// Vertical: limits.x = down (min), limits.y = up (max)
				limitVerticalSlider.value   = new Vector2(limits.x, limits.y);
				minLimitVerticalField.value = limits.x;
				maxLimitVerticalField.value = limits.y;

				limitVerticalSlider.RegisterValueChangedCallback(
					evt => {
						limits = new Vector4(evt.newValue.x, evt.newValue.y, limits.z, limits.w);
						minLimitVerticalField.SetValueWithoutNotify(evt.newValue.x);
						maxLimitVerticalField.SetValueWithoutNotify(evt.newValue.y);
					}
				);

				minLimitVerticalField.RegisterValueChangedCallback(
					evt => {
						limits = new Vector4(evt.newValue, limits.y, limits.z, limits.w);
						limitVerticalSlider.SetValueWithoutNotify(new Vector2(evt.newValue, limits.y));
					}
				);

				maxLimitVerticalField.RegisterValueChangedCallback(
					evt => {
						limits = new Vector4(limits.x, evt.newValue, limits.z, limits.w);
						limitVerticalSlider.SetValueWithoutNotify(new Vector2(limits.x, evt.newValue));
					}
				);
			}

			if (limitHorizontalSlider != null && minLimitHorizontalField != null && maxLimitHorizontalField != null) {
				// Horizontal: limits.z = left (min), limits.w = right (max)
				limitHorizontalSlider.value   = new Vector2(limits.z, limits.w);
				minLimitHorizontalField.value = limits.z;
				maxLimitHorizontalField.value = limits.w;

				limitHorizontalSlider.RegisterValueChangedCallback(
					evt => {
						limits = new Vector4(limits.x, limits.y, evt.newValue.x, evt.newValue.y);
						minLimitHorizontalField.SetValueWithoutNotify(evt.newValue.x);
						maxLimitHorizontalField.SetValueWithoutNotify(evt.newValue.y);
					}
				);

				minLimitHorizontalField.RegisterValueChangedCallback(
					evt => {
						limits = new Vector4(limits.x, limits.y, evt.newValue, limits.w);
						limitHorizontalSlider.SetValueWithoutNotify(new Vector2(evt.newValue, limits.w));
					}
				);

				maxLimitHorizontalField.RegisterValueChangedCallback(
					evt => {
						limits = new Vector4(limits.x, limits.y, limits.z, evt.newValue);
						limitHorizontalSlider.SetValueWithoutNotify(new Vector2(limits.z, evt.newValue));
					}
				);
			}

			// Configurer le champ du Transform cible
			if (targetField == null) return container;
			targetField.value = target;
			targetField.RegisterValueChangedCallback(evt => { target = evt.newValue as Transform; });

			return container;
		}
		#endif
	}
}