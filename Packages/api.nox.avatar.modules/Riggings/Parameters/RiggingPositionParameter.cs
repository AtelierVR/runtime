using Nox.Avatars.Parameters;
using Nox.CCK.Avatars.Parameters;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class RiggingPositionParameter : IParameter {
		private readonly HumanBodyBones      _bone;
		private readonly RiggingAvatarModule _module;
		private readonly string              _parameterName;

		public RiggingPositionParameter(HumanBodyBones bone, RiggingAvatarModule module) {
			_bone          = bone;
			_module        = module;
			_parameterName = $"tracking/{bone.ToString().ToSnakeCase()}/position";
		}

		public string GetName()
			=> _parameterName;

		public int GetHash()
			=> _parameterName.GetHashCode();

		public ParameterType GetValueType()
			=> ParameterType.Vector3;

		public bool IsReadOnly()
			=> false;

		public bool IsSyncable()
			=> true;

		public bool IsSavable()
			=> true;

		public object Get()
			=> _module?.GetPart(_bone)?.position ?? Vector3.zero;


		public void Set(object value) {
			if (!_module || value is not Vector3 position) return;
			_module.GetPart(_bone).position = position;
		}

		public byte[] Serialize()
			=> Get().ToBytes();

		public void Deserialize(byte[] data)
			=> Set(data);
	}
}