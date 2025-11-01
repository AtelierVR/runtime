using Nox.Avatars.Parameters;
using UnityEngine.Animations.Rigging;
using System.Linq;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class RigBuilderLayerActiveParameter : IParameter {
		private readonly RigBuilder _rigBuilder;
		private readonly string     _layerName;
		private readonly string     _parameterName;

		public RigBuilderLayerActiveParameter(string parameterName, string layerName, RigBuilder rigBuilder) {
			_parameterName = parameterName;
			_layerName     = layerName;
			_rigBuilder    = rigBuilder;
		}

		public string GetName()
			=> _parameterName;

		public int GetHash()
			=> _parameterName.GetHashCode();

		public ParameterType GetValueType()
			=> ParameterType.Bool;

		public ParameterFlags GetFlags()
			=> ParameterFlags.LocalEditable
				| ParameterFlags.RemoteEditableByLocal;

		public object Get() {
			if (!_rigBuilder) return false;
			var layer = _rigBuilder.layers.FirstOrDefault(l => l.rig && l.rig.name == _layerName);
			return layer is { active: true };
		}

		public void Set(object value) {
			if (!_rigBuilder) return;
			foreach (var layer in _rigBuilder.layers.Where(layer => layer.rig && layer.rig.name == _layerName)) {
				layer.active = value.ToBool();
				break;
			}
		}
	}
}