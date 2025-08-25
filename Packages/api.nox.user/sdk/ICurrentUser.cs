using System;

namespace Nox.Users {
	public interface ICurrentUser : IUser {
		public string GetEmail();

		public DateTime GetCreatedAt();

		public string GetHomeId();

		public string GetAvatarId();
	}
}