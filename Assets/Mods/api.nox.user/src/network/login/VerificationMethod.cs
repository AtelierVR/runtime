using System;
using Nox.CCK.Utils;

namespace api.nox.user.network {
	[Serializable]
	public class VerificationMethod : INoxObject {
		public string type;
		public string name;
		public bool enabled;

		public string GetMethodType()
			=> type;

		public string GetMethodName()
			=> name;

		public bool IsEnabled()
			=> enabled;

		public bool IsTotp()
			=> type == "totp";

		public bool IsEmail()
			=> type == "email";

		public override string ToString()
			=> $"{GetType().Name}[type={type}, name={name}, enabled={enabled}]";
	}
}
