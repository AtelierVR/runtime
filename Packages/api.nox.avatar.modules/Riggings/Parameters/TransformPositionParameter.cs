using Nox.Avatars.Parameters;
using UnityEngine;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class TransformPositionParameter : IParameter {
		private readonly Transform _transform;
		private readonly string    _name;
		private readonly bool      _isReadOnly;

		public TransformPositionParameter(string name, Transform transform, bool isReadOnly = false) {
			_name       = name;
			_transform  = transform;
			_isReadOnly = isReadOnly;
		}

		public string GetName()
			=> _name;

		public int GetHash()
			=> _name.GetHashCode();

		public ParameterType GetValueType()
			=> ParameterType.Vector3;

		public bool IsReadOnly()
			=> _isReadOnly;

		public bool IsSyncable()
			=> false;

		public bool IsSavable()
			=> true;

		public object Get() {
			return _transform != null ? _transform.position : Vector3.zero;
		}

		public void Set(object value) {
			if (_isReadOnly || _transform == null) return;

			if (value is Vector3 position) {
				_transform.position = position;
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