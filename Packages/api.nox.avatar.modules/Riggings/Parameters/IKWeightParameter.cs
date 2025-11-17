using Nox.Avatars.Parameters;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Linq;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class IKWeightParameter : IParameter {
		private readonly RigBuilder _rigBuilder;
		private readonly string     _rigName;
		private readonly string     _parameterName;
		private readonly WeightType _weightType;

		public enum WeightType {
			Position,
			Rotation,
			Hint
		}

		public IKWeightParameter(string parameterName, string rigName, WeightType weightType, RigBuilder rigBuilder) {
			_parameterName = parameterName;
			_rigName       = rigName;
			_weightType    = weightType;
			_rigBuilder    = rigBuilder;
		}

		public string GetName()
			=> _parameterName;

		public bool IsValid()
			=> _rigBuilder && _rigBuilder.layers.Any(l => l.rig && l.rig.name == _rigName);

		public int GetKey()
			=> _parameterName.GetHashCode();

		public ParameterType GetValueType()
			=> ParameterType.Float;

		public ParameterFlags GetFlags()
			=> ParameterFlags.LocalEditable
				| ParameterFlags.RemoteEditableByLocal;
		
		// ReSharper disable Unity.PerformanceAnalysis
		public object Get() {
			if (!_rigBuilder) return 0f;

			foreach (var layer in _rigBuilder.layers) {
				if (!layer.rig || !layer.rig.name.Contains(_rigName)) continue;

				var ikConstraints = layer.rig.GetComponentsInChildren<TwoBoneIKConstraint>();
				foreach (var constraint in ikConstraints) {
					return _weightType switch {
						WeightType.Position => constraint.data.targetPositionWeight,
						WeightType.Rotation => constraint.data.targetRotationWeight,
						WeightType.Hint     => constraint.data.hintWeight,
						_                   => 0f
					};
				}
			}

			return 0f;
		}

		public void Set(object value) {
			if (!_rigBuilder) return;

			var weight = value.ToFloat();

			foreach (var layer in _rigBuilder.layers) {
				if (!layer.rig || !layer.rig.name.Contains(_rigName)) continue;

				var ikConstraints = layer.rig.GetComponentsInChildren<TwoBoneIKConstraint>();
				foreach (var constraint in ikConstraints) {
					var data = constraint.data;
					switch (_weightType) {
						case WeightType.Position:
							data.targetPositionWeight = Mathf.Clamp01(weight);
							break;
						case WeightType.Rotation:
							data.targetRotationWeight = Mathf.Clamp01(weight);
							break;
						case WeightType.Hint:
							data.hintWeight = Mathf.Clamp01(weight);
							break;
					}

					constraint.data = data;
				}
			}
		}
	}
}