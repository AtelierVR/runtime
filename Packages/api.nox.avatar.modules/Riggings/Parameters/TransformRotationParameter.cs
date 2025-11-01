using Nox.Avatars.Parameters;
using UnityEngine;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class TransformRotationParameter : IParameter {
		private readonly Transform _transform;
		private readonly string    _name;
		private readonly bool      _isReadOnly;

		public TransformRotationParameter(string name, Transform transform, bool isReadOnly = false) {
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
			=> ParameterType.Quaternion;

		public bool IsReadOnly()
			=> _isReadOnly;

		public bool IsSyncable()
			=> false;

		public bool IsSavable()
			=> true;

		public object Get()
			=> _transform
				? _transform.rotation
				: Quaternion.identity;


		public void Set(object value) {
			if (_isReadOnly || !_transform) return;
			if (value is Quaternion rotation)
				_transform.rotation = rotation;
		}

		public byte[] Serialize()
			=> Get().ToBytes();

		public void Deserialize(byte[] data)
			=> Set(data);
	}
}