using System;
using Nox.Instances;
using Nox.Users;

namespace api.nox.instance {
	[Serializable]
	public class InstancePlayer : IPlayer {
		public string user;
		public string display;
		
		public IUserIdentifier GetIdentifier()
			=> Main.Instance.UserAPI.Make(user);

		public string GetDisplay()
			=> display;
	}
}