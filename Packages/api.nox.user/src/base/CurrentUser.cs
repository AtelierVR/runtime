using System;
using Cysharp.Threading.Tasks;
using Nox.Users;
using Nox.Worlds;

namespace api.nox.user {
	[Serializable]
	public class CurrentUser : User, ICurrentUser {
		public string email;
		public long   created_at;
		public string home;

		public string GetEmail()
			=> email;

		public DateTime GetCreatedAt()
			=> DateTimeOffset.FromUnixTimeMilliseconds(created_at)
				.UtcDateTime;

		public string GetHomeId()
			=> home;

		public UniTask<IWorld> GetHome()
			=> Main.Instance.WorldAPI.Fetch(home);
	}
}