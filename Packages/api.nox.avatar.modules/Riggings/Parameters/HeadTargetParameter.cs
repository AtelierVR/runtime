using Nox.Avatars.Parameters;
using Nox.CCK.Network;

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

		public ParameterFlags GetFlags()
			=> ParameterFlags.LocalEditable
				| ParameterFlags.RemoteEditableByLocal;

		public ParameterType GetValueType()
			=> ParameterType.Bool;

		public object Get()
			=> _target.enabled;

		public void Set(object value) {
			if (!_target) return;
			_target.enabled = value.ToBool();
		}
	}
}