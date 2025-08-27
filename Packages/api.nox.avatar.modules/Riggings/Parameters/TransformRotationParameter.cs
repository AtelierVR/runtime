using Nox.Avatars.Parameters;
using UnityEngine;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class TransformRotationParameter : IParameter {
		private readonly Transform _transform;
		private readonly string _name;
		private readonly bool _isReadOnly;
		
		public TransformRotationParameter(string name, Transform transform, bool isReadOnly = false) {
			_name = name;
			_transform = transform;
			_isReadOnly = isReadOnly;
		}

		public string GetName() => _name;
		public int GetHash() => _name.GetHashCode();
		public ParameterType GetValueType() => ParameterType.Quaternion;
		public bool IsReadOnly() => _isReadOnly;
		public bool IsSyncable() => false;
		public bool IsSavable() => true;

		public object Get() {
			return _transform != null ? _transform.rotation : Quaternion.identity;
		}

		public void Set(object value) {
			if (_isReadOnly || _transform == null) return;
			
			if (value is Quaternion rotation) {
				_transform.rotation = rotation;
			}
		}

		public T GetValue<T>() {
			var val = Get();
			if (val is T result) return result;
			return default(T);
		}

		public void SetValue<T>(T value) {
			Set((object)value);
		}
	}
}
