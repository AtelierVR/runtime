using Nox.Avatars.Parameters;
using Nox.CCK.Avatars.Parameters;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class RiggingRotationParameter : IParameter {
		private readonly HumanBodyBones      _bone;
		private readonly RiggingAvatarModule _module;
		private readonly string              _parameterName;

		public RiggingRotationParameter(HumanBodyBones bone, RiggingAvatarModule module) {
			_bone          = bone;
			_module        = module;
			_parameterName = $"tracking/{bone.ToString().ToSnakeCase()}/rotation";
		}

		public string GetName()
			=> _parameterName;

		public int GetHash()
			=> _parameterName.GetHashCode();

		public ParameterType GetValueType()
			=> ParameterType.Quaternion;

		public bool IsReadOnly()
			=> false;

		public bool IsSyncable()
			=> false;

		public bool IsSavable()
			=> true;

		public object Get()
			=> _module?.GetPart(_bone).rotation ?? Quaternion.identity;


		public void Set(object value) {
			if (!_module || value is not Quaternion rotation) return;
			_module.GetPart(_bone).rotation = rotation;
		}

		public byte[] Serialize()
			=> Get().ToBytes();

		public void Deserialize(byte[] data)
			=> Set(data);
	}
}