using Cysharp.Threading.Tasks;

namespace Nox.Users {
	public interface IUserAPI {
		public ICurrentUser GetCurrent();

		public UniTask<ICurrentUser> FetchCurrent();

		public UniTask<IUser> Fetch(IUserIdentifier identifier);

		public UniTask<IUser> Fetch(uint id, string from = null);

		public UniTask<IUser> Fetch(string identifier, string from = null);

		public IUserIdentifier Make(string identifier);

		public UniTask<ISearchResponse> Search(ISearchRequest request);

		public UniTask<IAuthToken> GetToken(string address);
	}
}