using Nox.Avatars.Parameters;
using Nox.CCK.Avatars.Parameters;
using UnityEngine;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class HeadTargetParameter : IParameter {
		private readonly string     _parameterName;
		private readonly HeadTarget _target;

		public HeadTargetParameter(string s, HeadTarget target) {
			_parameterName = s;
			_target        = target;
		}

		public string GetName()
			=> _parameterName;

		public int GetHash()
			=> _parameterName.GetHashCode();

		public ParameterType GetValueType()
			=> ParameterType.Bool;

		public bool IsReadOnly()
			=> false;

		public bool IsSyncable()
			=> true;

		public bool IsSavable()
			=> true;

		public byte[] Serialize()
			=> Get().ToBytes();

		public void Deserialize(byte[] data)
			=> Set(data);

		public object Get()
			=> _target.enabled;

		public void Set(object value) {
			if (!_target) return;
			_target.enabled = value.ToBool();
		}
	}
}