using System.Linq;
using Newtonsoft.Json.Linq;
using Nox.CCK.Utils;
using Nox.Users;

namespace api.nox.user.network {
	public class LoginRequest : ILoginRequest, INoxObject {
		internal string Identifier;
		internal string Password;

		public string ToJson()
			=> new JObject {
				["identifier"] = Identifier,
				["password"]   = Hashing.Sha256(Password)
			}.ToString();

		public override string ToString()
			=> $"{GetType().Name}[identifier={Identifier}, password={string.Join("", Password.Split().Select(c => '*'))}]";

		public ILoginRequest SetPassword(string password) {
			Password = password;
			return this;
		}

		public ILoginRequest SetIdentifier(string identifier) {
			Identifier = identifier;
			return this;
		}

		public string GetPassword()
			=> Password;

		public string GetIdentifier()
			=> Identifier;
	}
}