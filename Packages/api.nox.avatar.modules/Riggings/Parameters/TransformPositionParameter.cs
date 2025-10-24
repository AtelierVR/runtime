using Nox.Avatars.Parameters;
using Nox.CCK.Avatars.Parameters;
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

		public bool IsValid()
			=> _transform;

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

		public object Get()
			=> _transform
				? _transform.position
				: Vector3.zero;


		public void Set(object value) {
			if (_isReadOnly || !_transform) return;
			if (value is Vector3 position) 
				_transform.position = position;
		}

		public byte[] Serialize()
			=> Get().ToBytes();

		public void Deserialize(byte[] data)
			=> Set(data);
	}
}