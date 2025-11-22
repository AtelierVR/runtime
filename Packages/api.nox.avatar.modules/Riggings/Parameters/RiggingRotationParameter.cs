using Nox.Avatars.Parameters;
using Nox.CCK.Utils;
using UnityEngine;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class RiggingRotationParameter : IParameter {
		private readonly HumanBodyBones      _bone;
		private readonly BaseRiggingModule _module;
		private readonly string              _parameterName;

		public RiggingRotationParameter(HumanBodyBones bone, BaseRiggingModule module) {
			_bone          = bone;
			_module        = module;
			_parameterName = $"tracking/{bone.ToString().ToSnakeCase()}/rotation";
		}

		public string GetName()
			=> _parameterName;

		public bool IsValid()
			=> _module && _module.GetPart(_bone);

		public int GetKey()
			=> _parameterName.GetHashCode();

		public ParameterType GetValueType()
			=> ParameterType.Quaternion;

		public ParameterFlags GetFlags()
			=> ParameterFlags.Persistent;


		public object Get()
			=> _module?.GetPart(_bone).rotation ?? Quaternion.identity;


		public void Set(object value) {
			if (!_module) return;
			_module.GetPart(_bone).rotation = value.ToQuaternion();
		}
	}
}