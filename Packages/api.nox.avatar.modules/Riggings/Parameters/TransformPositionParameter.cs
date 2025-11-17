using Nox.Avatars.Parameters;
using UnityEngine;
using Nox.CCK.Network;

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

		public bool IsValid()
			=> _transform;

		public int GetKey()
			=> _name.GetHashCode();

		public ParameterType GetValueType()
			=> ParameterType.Vector3;

		public ParameterFlags GetFlags()
			=> ParameterFlags.Persistent;

		public object Get()
			=> _transform
				? _transform.position
				: Vector3.zero;


		public void Set(object value) {
			if (_isReadOnly || !_transform) return;
			_transform.position = value.ToVector3();
		}
	}
}