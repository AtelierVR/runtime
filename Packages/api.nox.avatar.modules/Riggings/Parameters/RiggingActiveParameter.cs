using Nox.Avatars.Parameters;
using Nox.CCK.Utils;
using UnityEngine;
using Nox.CCK.Network;

namespace Nox.CCK.Avatars.Rigging.Parameters {
	public class RiggingActiveParameter : IParameter {
		private readonly HumanBodyBones    _bone;
		private readonly BaseRiggingModule _module;
		private readonly string            _parameterName;

		public RiggingActiveParameter(HumanBodyBones bone, BaseRiggingModule module) {
			_bone          = bone;
			_module        = module;
			_parameterName = $"tracking/{bone.ToString().ToSnakeCase()}/active";
		}

		public string GetName()
			=> _parameterName;

		public bool IsValid()
			=> _module && _module.GetPart(_bone);

		public int GetKey()
			=> _parameterName.GetHashCode();

		public ParameterType GetValueType()
			=> ParameterType.Bool;

		public ParameterFlags GetFlags()
			=> ParameterFlags.LocalEditable
				| ParameterFlags.RemoteEditableByLocal;

		public object Get()
			=> _module && _module.IsActive(_bone);

		public void Set(object value) {
			if (!_module) return;
			_module.SetActive(_bone, value.ToBool());
		}
	}
}