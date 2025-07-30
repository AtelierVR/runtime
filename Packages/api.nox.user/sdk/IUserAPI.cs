using Cysharp.Threading.Tasks;

namespace Nox.Users {
	public interface IUserAPI {
		public ICurrentUser GetCurrent();

		public UniTask<ICurrentUser> FetchCurrent();

		public UniTask<IUser> Fetch(IUserIdentifier identifier);

		public UniTask<IUser> Fetch(uint id, string from = null);

		public UniTask<IUser> Fetch(string identifier, string from = null);

		public ISearchRequest MakeSearchRequest();

		public IUserIdentifier Make(string identifier);

		public IUserIdentifier Make(uint id, string server = "::");

		public UniTask<ISearchResponse> Search(ISearchRequest request, string from = null);

		public UniTask<IAuthToken> GetToken(string address);

		public UniTask<ICurrentUser> UpdateCurrent(IUpdateCurrentUserRequest request);

		public IUpdateCurrentUserRequest MakeUpdateCurrentRequest();
	}
}