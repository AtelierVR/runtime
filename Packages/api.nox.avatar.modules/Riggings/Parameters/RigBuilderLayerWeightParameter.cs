using Nox.Avatars.Parameters;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Linq;
using Nox.CCK.Network;

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

		public bool IsValid()
			=> _rigBuilder && _rigBuilder.enabled && _rigBuilder.layers.Any(l => l.rig && l.rig.name == _layerName);

		public int GetKey()
			=> _parameterName.GetHashCode();

		public ParameterType GetValueType()
			=> ParameterType.Float;

		public ParameterFlags GetFlags()
			=> ParameterFlags.LocalEditable
				| ParameterFlags.RemoteEditableByLocal;

		public object Get() {
			if (!_rigBuilder) return 0f;
			var layer = _rigBuilder.layers.FirstOrDefault(l => l.rig && l.rig.name == _layerName);
			return layer?.rig?.weight ?? 0f;
		}

		public void Set(object value) {
			if (!_rigBuilder || !_rigBuilder.enabled) return;
			
			foreach (var layer in _rigBuilder.layers.Where(layer => layer.rig && layer.rig.name == _layerName)) {
				layer.rig.weight = Mathf.Clamp01(value.ToFloat());
				break;
			}
			
			// Only rebuild if safe to do so  
			if (Application.isPlaying && _rigBuilder.isActiveAndEnabled) {
				try {
					_rigBuilder.Build();
				}
				catch (System.InvalidOperationException ex) when (ex.Message.Contains("TransformStreamHandle")) {
					// Silently handle timing issues - build will happen later
					UnityEngine.Debug.LogWarning($"RigBuilder build skipped due to timing: {ex.Message}");
				}
			}
		}
	}
}