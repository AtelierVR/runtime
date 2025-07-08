using Nox.CCK.Utils;
using Nox.Users;

namespace api.nox.relay.types.Authentication {
	public class RelayRequestAuthentication : RelayRequest {
		public AuthenticationFlags Flags;
		public string              Token;

		public void SetIntegrity(string token) {
			Flags = AuthenticationFlags.UseIntegrity;
			Token = token;
		}

		public void SetAuth(IAuthToken authToken) {
			if (authToken == null) SetGuest();
			else if (authToken.IsIntegrity()) SetIntegrity(authToken.GetToken());
			else SetToken(authToken.GetToken());
		}

		public void SetGuest() {
			Flags = AuthenticationFlags.UseGuest;
			Token = "";
		}

		public void SetToken(string token) {
			Flags = AuthenticationFlags.None;
			Token = token;
		}

		public static RelayRequestAuthentication CreateIntegrity(string token) {
			var request = new RelayRequestAuthentication();
			request.SetIntegrity(token);
			return request;
		}

		public static RelayRequestAuthentication CreateGuest() {
			var request = new RelayRequestAuthentication();
			request.SetGuest();
			return request;
		}

		public static RelayRequestAuthentication CreateToken(string token) {
			var request = new RelayRequestAuthentication();
			request.SetToken(token);
			return request;
		}

		public static RelayRequestAuthentication CreateAuth(IAuthToken authToken) {
			var request = new RelayRequestAuthentication();
			request.SetAuth(authToken);
			return request;
		}

		public override Buffer ToBuffer() {
			var buffer = new Buffer();
			buffer.Write(Flags);
			if (Flags is not AuthenticationFlags.UseGuest)
				buffer.Write(Token);
			return buffer;
		}
	}
}