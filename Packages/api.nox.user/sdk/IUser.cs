using Cysharp.Threading.Tasks;

namespace Nox.Users {
	public interface IUser {
		public uint            GetId();
		public string          GetDisplay();
		public string          GetUsername();
		public string          GetBio();
		public string          GetServerAddress();
		public string          GetThumbnailUrl();
		public string          GetBannerUrl();
		public IEntry[]        GetLinks();
		public IRelationship   GetRelationships();
		public string[]        GetTags();
		public float           GetRank();
		public UniTask<IUser>  Refresh();
		public IUserIdentifier ToIdentifier();
		public string          GetCertificate();
		public int             GetFollowers();
		public int             GetFollowing();
	}
}