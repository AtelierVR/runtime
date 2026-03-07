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
		public string avatar;

		public string GetEmail()
			=> email;

		public DateTime GetCreatedAt()
			=> DateTimeOffset.FromUnixTimeMilliseconds(created_at)
				.UtcDateTime;

		public string GetHomeId()
			=> home;

		public string GetAvatarId()
			=> avatar;

		public override string ToString()
			=> $"{GetType().Name}[id={ToIdentifier().ToString(server)}, username={GetUsername()}]";
	}
}