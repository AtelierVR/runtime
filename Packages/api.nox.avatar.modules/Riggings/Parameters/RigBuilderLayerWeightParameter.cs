using Nox.Avatars.Parameters;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Linq;
using Nox.CCK.Avatars.Parameters;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class RigBuilderLayerWeightParameter : IParameter {
		private readonly RigBuilder _rigBuilder;
		private readonly string     _layerName;
		private readonly string     _parameterName;

		public RigBuilderLayerWeightParameter(string parameterName, string layerName, RigBuilder rigBuilder) {
			_parameterName = parameterName;
			_layerName     = layerName;
			_rigBuilder    = rigBuilder;
		}

		public string GetName()
			=> _parameterName;

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

		public object Get() {
			if (!_rigBuilder) return 0f;
			var layer = _rigBuilder.layers.FirstOrDefault(l => l.rig && l.rig.name == _layerName);
			return layer?.rig?.weight ?? 0f;
		}

		public void Set(object value) {
			if (!_rigBuilder || value is not float weight) return;
			foreach (var layer in _rigBuilder.layers.Where(layer => layer.rig && layer.rig.name == _layerName)) {
				layer.rig.weight = Mathf.Clamp01(weight);
				break;
			}
		}

		public byte[] Serialize()
			=> Get().ToBytes();

		public void Deserialize(byte[] data)
			=> Set(data);
	}
}