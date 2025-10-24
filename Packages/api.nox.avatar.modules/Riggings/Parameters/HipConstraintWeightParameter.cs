using Nox.Avatars.Parameters;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Linq;
using Nox.CCK.Avatars.Parameters;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class HipConstraintWeightParameter : IParameter {
		private readonly RigBuilder     _rigBuilder;
		private readonly string         _parameterName;
		private readonly ConstraintType _constraintType;

		public enum ConstraintType {
			Position,
			Rotation
		}

		public HipConstraintWeightParameter(string parameterName, ConstraintType constraintType, RigBuilder rigBuilder) {
			_parameterName  = parameterName;
			_constraintType = constraintType;
			_rigBuilder     = rigBuilder;
		}

		public string GetName()
			=> _parameterName;

		public bool IsValid()
			=> _rigBuilder && _rigBuilder.layers.Any(l => l.rig && l.rig.name.Contains("Hip"));

		public int GetHash()
			=> _parameterName.GetHashCode();

		public ParameterType GetValueType()
			=> ParameterType.Float;

		public bool IsReadOnly()
			=> false;

		public bool IsSyncable()
			=> false;

		public bool IsSavable()
			=> true;

		public byte[] Serialize()
			=> Get().ToBytes();

		public void Deserialize(byte[] data)
			=> Set(data);

		// ReSharper disable Unity.PerformanceAnalysis
		public object Get() {
			if (_rigBuilder == null) return 0f;

			foreach (var layer in _rigBuilder.layers) {
				if (!layer.rig || !layer.rig.name.Contains("Hip")) continue;

				if (_constraintType == ConstraintType.Position) {
					var positionConstraint = layer.rig.GetComponentInChildren<MultiPositionConstraint>();
					if (positionConstraint) return positionConstraint.weight;
				} else {
					var rotationConstraint = layer.rig.GetComponentInChildren<MultiRotationConstraint>();
					if (rotationConstraint) return rotationConstraint.weight;
				}
			}

			return 0f;
		}

		// ReSharper disable Unity.PerformanceAnalysis
		public void Set(object value) {
			if (_rigBuilder == null || !(value is float weight)) return;

			foreach (var layer in _rigBuilder.layers) {
				if (!layer.rig || !layer.rig.name.Contains("Hip")) continue;

				if (_constraintType == ConstraintType.Position) {
					var positionConstraint                            = layer.rig.GetComponentInChildren<MultiPositionConstraint>();
					if (positionConstraint) positionConstraint.weight = Mathf.Clamp01(weight);
				} else {
					var rotationConstraint                            = layer.rig.GetComponentInChildren<MultiRotationConstraint>();
					if (rotationConstraint) rotationConstraint.weight = Mathf.Clamp01(weight);
				}
			}
		}
	}
}