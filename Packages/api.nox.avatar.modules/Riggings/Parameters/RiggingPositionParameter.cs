using Nox.Avatars.Parameters;
using Nox.CCK.Utils;
using UnityEngine;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class RiggingPositionParameter : IParameter {
		private readonly HumanBodyBones      _bone;
		private readonly BaseRiggingModule _module;
		private readonly string              _parameterName;

		public RiggingPositionParameter(HumanBodyBones bone, BaseRiggingModule module) {
			_bone          = bone;
			_module        = module;
			_parameterName = $"tracking/{bone.ToString().ToSnakeCase()}/position";
		}

		public string GetName()
			=> _parameterName;

		public bool IsValid()
			=> _module && _module.GetPart(_bone);

		public int GetKey()
			=> _parameterName.GetHashCode();

		public ParameterType GetValueType()
			=> ParameterType.Vector3;

		public ParameterFlags GetFlags()
			=> ParameterFlags.Persistent;

		public object Get()
			=> _module?.GetPart(_bone)?.position ?? Vector3.zero;


		public void Set(object value) {
			if (!_module) return;
			_module.GetPart(_bone).position = value.ToVector3();
		}
	}
}