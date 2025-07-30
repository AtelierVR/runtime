using Nox.Users;

namespace Nox.Instances {
	public interface IPlayer {
		public IUserIdentifier GetIdentifier();

		public string GetDisplay();
	}
}