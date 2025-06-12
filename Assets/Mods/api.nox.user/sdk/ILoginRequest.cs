namespace Nox.Users {
	public interface ILoginRequest {
		public ILoginRequest SetPassword(string   password);
		public ILoginRequest SetIdentifier(string identifier);

		public string GetPassword();
		public string GetIdentifier();
	}
}