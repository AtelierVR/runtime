using System;
using System.Linq;
using System.Runtime.InteropServices;
using Nox.Avatars.Parameters;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Avatars.StateMachines {
	public class LerpParameter : BaseStateMachine {
		public string        key;
		public ParameterType type;
		public byte[]        targetValue;
		public float         duration = 1f;

		private IParameter _parameter;
		private float      _elapsed;
		private object     _startValue;
		private object     _targetObj;

		private int GetKeyHash()
			=> Animator.StringToHash(key);

		public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
			base.OnStateEnter(animator, stateInfo, layerIndex);
			_elapsed    = 0f;
			_startValue = null;
			_targetObj  = DeserializeTarget();

			var module = RuntimeAvatar?.GetDescriptor()
				.GetModules<IParameterModule>()
				.FirstOrDefault();

			if (module == null) {
				Logger.LogWarning("No ParameterModule found on avatar, the parameter won't be lerped.");
				return;
			}

			_parameter = module.GetParameter(GetKeyHash());
			if (_parameter == null) {
				Logger.LogWarning($"Parameter '{key}' not found on avatar.");
				return;
			}

			// Try to get the start value via reflection (GetValue() or Value)
			try {
				var pType     = _parameter.GetType();
				var getMethod = pType.GetMethod("GetValue", Type.EmptyTypes) ?? pType.GetMethod("Get", Type.EmptyTypes);
				if (getMethod != null) {
					_startValue = getMethod.Invoke(_parameter, null);
				} else {
					var prop                     = pType.GetProperty("Value");
					if (prop != null) _startValue = prop.GetValue(_parameter);
				}
			} catch (Exception e) {
				Logger.LogWarning($"Failed to read start value for '{key}': {e.Message}");
			}

			// Fallback defaults by parameter type
			_startValue ??= type switch {
				ParameterType.Float      => 0f,
				ParameterType.Vector3    => Vector3.zero,
				ParameterType.Quaternion => Quaternion.identity,
				_                        => null
			};
		}

		public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
			base.OnStateUpdate(animator, stateInfo, layerIndex);
			if (_parameter == null || _targetObj == null) return;
			if (duration <= 0f) {
				_parameter.Set(_targetObj);
				return;
			}

			_elapsed += Time.deltaTime;
			var t = Mathf.Clamp01(_elapsed / duration);

			var current = type switch {
				ParameterType.Float when _startValue is float svf          && _targetObj is float tvf     => Mathf.Lerp(svf, tvf, t),
				ParameterType.Vector3 when _startValue is Vector3 sv3      && _targetObj is Vector3 tv3   => Vector3.Lerp(sv3, tv3, t),
				ParameterType.Quaternion when _startValue is Quaternion sq && _targetObj is Quaternion tq => Quaternion.Slerp(sq, tq, t),
				_                                                                                       => _targetObj
			};

			try {
				_parameter.Set(current);
			} catch (Exception e) {
				Logger.LogError($"Failed to lerp parameter '{key}': {e.Message}");
			}
		}

		public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
			base.OnStateExit(animator, stateInfo, layerIndex);
			if (_parameter == null || _targetObj == null) return;

			try {
				_parameter.Set(_targetObj); // ensure final value
			} catch (Exception e) {
				Logger.LogError($"Failed to set final value for '{key}': {e.Message}");
			}
		}

		private object DeserializeTarget() {
			if (targetValue == null || targetValue.Length == 0) return null;

			return type switch {
				ParameterType.Float      => MemoryMarshal.Read<float>(targetValue),
				ParameterType.Vector3    => MemoryMarshal.Read<Vector3>(targetValue),
				ParameterType.Quaternion => MemoryMarshal.Read<Quaternion>(targetValue),
				_                        => null
			};
		}
	}
}