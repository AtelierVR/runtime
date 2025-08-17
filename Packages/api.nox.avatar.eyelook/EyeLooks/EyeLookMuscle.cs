using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
#endif

namespace Nox.CCK.Avatars.EyeLooks {
	[Serializable, Tooltip("Muscle")]
	public class EyeLookMuscle : BaseEyeLook {
		public override void UpdateEye(EyeLookContext context, Vector4 direction) {
			var animator = context.Descriptor?.GetAnimator();
			if (!animator) return;
			var lookDirection = new Vector3(
				direction.w - direction.z, // left/right
				direction.y - direction.x, // up/down
				1f                         // forward
			).normalized;
			var lookPosition = animator.transform.position + lookDirection;
			animator.SetLookAtPosition(lookPosition);
			animator.SetLookAtWeight(
				1f,  // weight (0-1)
				1f,  // bodyWeight (0-1)
				1f,  // headWeight (0-1)
				1f,  // eyesWeight (0-1)
				0.5f // clampWeight (0-1)
			);
		}

		#if UNITY_EDITOR
		public override VisualElement CreateInspectorGUI(EyeLookContext context) {
			// Charger le fichier UXML spécifique à EyeLookMuscle
			var visualTree = Resources.Load<VisualTreeAsset>("EyeLookMuscle");
			if (visualTree == null) {
				Debug.LogError("Could not load EyeLookMuscle.uxml from Resources folder");
				return base.CreateInspectorGUI(context); // Fallback vers l'interface de base
			}
			
			var container = visualTree.CloneTree();
			
			// Récupérer les éléments UI pour les limites
			var limitVerticalSlider = container.Q<MinMaxSlider>("limit-vertical");
			var limitHorizontalSlider = container.Q<MinMaxSlider>("limit-horizontal");
			var minLimitVerticalField = container.Q<FloatField>("min-limit-vertical");
			var maxLimitVerticalField = container.Q<FloatField>("max-limit-vertical");
			var minLimitHorizontalField = container.Q<FloatField>("min-limit-horizontal");
			var maxLimitHorizontalField = container.Q<FloatField>("max-limit-horizontal");
			
			// Configurer les MinMaxSliders et FloatFields des limites
			if (limitVerticalSlider != null && minLimitVerticalField != null && maxLimitVerticalField != null) {
				// Vertical: limits.x = down (min), limits.y = up (max)
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
				// Horizontal: limits.z = left (min), limits.w = right (max)
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
			
			return container;
		}
		#endif
	}
}